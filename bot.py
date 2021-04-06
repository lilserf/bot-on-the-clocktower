import os

from dotenv import load_dotenv
import datetime
import discord
from discord.ext import commands, tasks
import json
from operator import itemgetter, attrgetter
import pymongo
from pymongo import MongoClient
import random
import shlex
import traceback

load_dotenv()
TOKEN = os.getenv('DISCORD_TOKEN')
if TOKEN is None:
    raise Exception("No DISCORD_TOKEN found. Be sure you have DISCORD_TOKEN defined in your environment")
intents = discord.Intents().default()
intents.members = True

###########
# Required permissions:
#
# Manage Roles, View Channels, Change Nicknames, Manage Nicknames
# Send Messages, Manage Messages
# Move Members

################
# Connect to mongo and get our DB object used globally throughout this file
# TODO: Make this not a big ol' global?
MONGO_CONNECT = os.getenv('MONGO_CONNECT')
if MONGO_CONNECT is None:
    raise Exception("No MONGO_CONNECT string found. Be sure you have MONGO_CONNECT defined in your environment")
cluster = MongoClient(MONGO_CONNECT)
db = cluster['botc']

g_dbGuildInfo = db['GuildInfo']
g_dbActiveGames = db['ActiveGames']

# Helpers
def getChannelFromCategoryByName(category, name):
    return discord.utils.find(lambda c: (c.type == discord.ChannelType.voice or c.type == discord.ChannelType.text) and c.name == name, category.channels)

def getCategoryByName(guild, name):
    return discord.utils.find(lambda c: c.type == discord.ChannelType.category and c.name == name, guild.channels)

def getRoleByName(guild, name):
    return discord.utils.find(lambda r: r.name==name, guild.roles)

# Nicer than a dict for storing info about the town
class TownInfo:
    def __init__(self, guild, document):

        if document:
            self.dayCategory = getCategoryByName(guild, document["dayCategory"])
            self.nightCategory = getCategoryByName(guild, document["nightCategory"])

            self.townSquare = getChannelFromCategoryByName(self.dayCategory, document["townSquare"])
            self.controlChannel = getChannelFromCategoryByName(self.dayCategory, document["controlChannel"])

            self.dayChannels = list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id ==  self.dayCategory.id)
            self.nightChannels = list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id ==  self.nightCategory.id)

            self.storyTellerRole = getRoleByName(guild, document["storyTellerRole"])
            self.villagerRole = getRoleByName(guild, document["villagerRole"])

            activePlayers = set()
            for c in self.dayChannels:
                activePlayers.update(c.members)
            for c in self.nightChannels:
                activePlayers.update(c.members)
            self.activePlayers = activePlayers

            self.storyTellers = set()
            for p in self.activePlayers:
                if self.storyTellerRole in p.roles:
                    self.storyTellers.add(p)

            self.authorName = document["authorName"]
            self.timestamp = document["timestamp"]

# Bot subclass
class botcBot(commands.Bot):

    # Get a well-defined TownInfo based on the stored DB info for this guild
    def getTownInfo(self, ctx):

        query = { "guild" : ctx.guild.id, "controlChannelId" : ctx.channel.id }
        doc = g_dbGuildInfo.find_one(query)

        if doc:
            return TownInfo(ctx.guild, doc)
        else:
            return None
    
    # Helper to send a message to the author of the command about what they did wrong
    async def sendErrorToAuthor(self, ctx, error=None):
        if error is not None:
            formatted = error
        else:
            formatted = '```\n' + traceback.format_exc(3) + '\n```'
            traceback.print_exc()
        await ctx.author.send(f"Alas, an error has occurred:\n{formatted}\n(from message `{ctx.message.content}`)")


    async def on_ready(self):
        print(f'{self.user.name} has connected to Discord!')

# Setup cog
class Setup(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    @commands.command(name='townInfo', aliases=['towninfo'], help='Show the stored info about the channels and roles that make up this town')
    async def townInfo(self, ctx):

        info = self.bot.getTownInfo(ctx)

        await self.sendEmbed(ctx, info)    
    
    
    async def addTownInternal(self, ctx, post, info, message_if_exists=True):
        guild = ctx.guild
        
        # Check if a town already exists
        query = { 
            "guild" : post["guild"],
            "dayCategoryId" : post["dayCategoryId"],
        }

        if message_if_exists:
            existing = g_dbGuildInfo.find_one(query)
            if existing:
                await ctx.send(f'Found an existing town on this server using daytime category `{post["dayCategory"]}`, modifying it!')

        # Upsert the town into place
        print(f'Adding a town to guild {post["guild"]} with control channel [{post["controlChannel"]}], day category [{post["dayCategory"]}], night category [{post["nightCategory"]}]')
        g_dbGuildInfo.replace_one(query, post, True)

        await self.sendEmbed(ctx, info)

    @commands.command(name='addTown', aliases=['addtown'], help='Add a game on this server.\nUsage: !addTown <control channel> <town square channel> <day category> <night category> <storyteller role> <villager role>')
    async def addTown(self, ctx):
        params = shlex.split(ctx.message.content)

        (post, info) = await self.resolveTownInfoParams(ctx, params)

        if not post:
            return

        await self.addTownInternal(ctx, post, info)


    @commands.command(name='removeTown', aliases=['removetown'], help='Remove a game on this server')
    async def removeTown(self, ctx):
        guild = ctx.guild

        params = shlex.split(ctx.message.content)

        post = {"guild" : guild.id,
            "dayCategory" : params[1]}

        print(f'Removing a game from guild {post["guild"]} with day category [{post["dayCategory"]}]')
        g_dbGuildInfo.delete_one(post)

        embed = discord.Embed(title=f'{guild.name} // {post["dayCategory"]}', description=f'Deleted!', color=0xcc0000)
        await ctx.send(embed=embed)


    # Parse all the params for addTown, sanity check them, and return useful dicts
    async def resolveTownInfoParams(self, ctx, params):

        if len(params) < 7:
            await ctx.send("Too few params to `!addTown`: should provide `<control channel> <townsquare channel> <day category> <night category> <ST role> <player role>`")
            return None

        controlName = params[1]
        townSquareName = params[2]
        dayCatName = params[3]
        nightCatName = params[4]
        stRoleName = params[5]
        villagerName = params[6]
        
        await self.resolveTownInfo(ctx, controlName, townSquareName, dayCatName, nightCatName, stRoleName, villagerName)


    # Using passed-in name params, resolve town info and find all the stuff needed to post to DB
    async def resolveTownInfo(self, ctx, controlName, townSquareName, dayCatName, nightCatName, stRoleName, villagerName):
        guild = ctx.guild
        
        dayCat = getCategoryByName(guild, dayCatName)
        nightCat = getCategoryByName(guild, nightCatName)
        
        controlChan = getChannelFromCategoryByName(dayCat, controlName)
        townSquare = getChannelFromCategoryByName(dayCat, townSquareName)

        stRole = getRoleByName(guild, stRoleName)
        villagerRole = getRoleByName(guild, villagerName)

        # TODO: more checks
        # does the night category contain some channels to distribute people to?
        
        # TODO: also specify the roles to set
        # does the storyteller role have perms to see all the cottages?
        # do the player roles NOT have perms to see the cottages

        if not dayCat:
            await ctx.send(f'Couldn\'t find a category named `{dayCatName}`!')
            return None

        if not nightCat:
            await ctx.send(f'Couldn\'t find a category named `{nightCatName}`!')
            return None

        if not controlChan:
            await ctx.send(f'Couldn\'t find a channel named `{controlName}` in category `{dayCatName}`!')
            return None

        if not townSquare:
            await ctx.send(f'Couldn\'t find a channel named `{townSquareName}` in category `{dayCatName}`!')
            return None

        if not stRole:
            await ctx.send(f'Couldn\'t find a role named `{stRoleName}`!')
            return None

        if not villagerRole:
            await ctx.send(f'Couldn\'t find a role named `{villagerName}`!')
            return None

        # Object suitable for mongo
        post = {   
            "guild" : guild.id,
            "controlChannel" : controlName,
            "controlChannelId" : controlChan.id,
            "townSquare" : townSquareName,
            "townSquareId" : townSquare.id,
            "dayCategory" : dayCatName,
            "dayCategoryId" : dayCat.id,
            "nightCategory" : nightCatName,
            "nightCategoryId" : nightCat.id,
            "storyTellerRole" : stRoleName,
            "storyTellerRoleId" : stRole.id,
            "villagerRole" : villagerName,
            "villagerRoleId" : villagerRole.id,
            "authorName" : ctx.author.display_name,
            "author" : ctx.author.id,
            "timestamp" : ctx.message.created_at
        }

        objs = TownInfo(ctx, post)

        return (post, objs)


    @commands.command(name='createTown', aliases=['createtown'], help='Create an entire town on this server, including categories, roles, channels, and permissions')
    async def createTown(self, ctx):
        params = shlex.split(ctx.message.content)

        guild = ctx.guild
        
        usageStr = "Usage: `<town name> [server storyteller role] [server player role]`"

        if len(params) < 2:
            await ctx.send("Too few params to `!createTown`. " + usageStr)
            return None

        townName = params[1]        
        if not townName:
            await ctx.send("No town name provided. " + usageStr)
            return None
        
        # Ensure all the roles exist
        botRole = getRoleByName(guild, self.bot.user.name)
        if not botRole:
            await ctx.send("Could not find role for **" + self.bot.user.name + "**. Cannot proceed! Where did the role go?")
            return None
        
        guildStRole = None
        if len(params) > 2:
            guildStRole = getRoleByName(guild, params[2])
            if not guildStRole:
                await ctx.send("Provided Storyteller Role **" + params[2] + "** not found.")
                return None
                
        guildPlayerRole = None
        if len(params) > 3:
            guildPlayerRole = getRoleByName(guild, params[3])
            if not guildPlayerRole:
                await ctx.send("Provided Player Role **" + params[3] + "** not found.")
                return None
        
        # These are in sync with those in destroyTown, could probably stand to abstract somehow
        dayCatName = townName
        nightCatName = townName + " - Night"
        gameStRoleName = townName + " Storyteller"
        gameVillagerRoleName = townName + " Villager"
        moverChannelName = "botc_mover"
        chatChannelName = "chat"
        townSquareChannelName = "Town Square"
        extraChannelNames = ["Dark Alley", "Library", "Graveyard"]
        nightChannelName = "Cottage"
        neededNightChannels = 20


        try:
            await ctx.send("Please hold, creating **" + townName + "** ...")
            
            # Roles
            everyoneRole = getRoleByName(guild, "@everyone")
            if not everyoneRole:
                await ctx.send("Could not find the **@everyone** role. Why not?")
                return None
        
            gameVillagerRole = getRoleByName(guild, gameVillagerRoleName)
            if not gameVillagerRole:
                gameVillagerRole = await guild.create_role(name=gameVillagerRoleName, color=discord.Color.magenta())
                
            gameStRole = getRoleByName(guild, gameStRoleName)
            if not gameStRole:
                gameStRole = await guild.create_role(name=gameStRoleName, color=discord.Color.dark_magenta()) 


            # Day category
            dayCat = getCategoryByName(guild, dayCatName)
            if not dayCat:
                dayCat = await guild.create_category(dayCatName)
            
            await dayCat.set_permissions(gameVillagerRole, view_channel=True)
            await dayCat.set_permissions(botRole, view_channel=True, move_members=True)


            # Night category
            nightCat = getCategoryByName(guild, nightCatName)
            if not nightCat:
                nightCat = await guild.create_category(nightCatName)

            await nightCat.set_permissions(gameStRole, view_channel=True)
            await nightCat.set_permissions(botRole, view_channel=True, move_members=True)
            await nightCat.set_permissions(everyoneRole, view_channel=False)


            # Mover channel
            moverChannel = getChannelFromCategoryByName(dayCat, moverChannelName)
            if not moverChannel:
                moverChannel = await dayCat.create_text_channel(moverChannelName)
            await moverChannel.set_permissions(botRole, view_channel=True)
            await moverChannel.set_permissions(gameVillagerRole, overwrite=None)
            
            if guildStRole:
                await moverChannel.set_permissions(guildStRole, view_channel=True)
                await moverChannel.set_permissions(everyoneRole, view_channel=False)


            # Chat channel
            chatChannel = getChannelFromCategoryByName(dayCat, chatChannelName)
            if not chatChannel:
                chatChannel = await dayCat.create_text_channel(chatChannelName)
            if not guildPlayerRole:
                await chatChannel.set_permissions(everyoneRole, view_channel=False)


            # Town Square 
            townSquareChannel = getChannelFromCategoryByName(dayCat, townSquareChannelName)
            if not townSquareChannel:
                townSquareChannel = await dayCat.create_voice_channel(townSquareChannelName)

            if guildPlayerRole:
                await dayCat.set_permissions(everyoneRole, view_channel=False)
                await townSquareChannel.set_permissions(guildPlayerRole, view_channel=True)
            
            
            # Extra day channels
            for extraChannelName in extraChannelNames:
                extraChannel = getChannelFromCategoryByName(dayCat, extraChannelName)
                if not extraChannel:
                    extraChannel = await dayCat.create_voice_channel(extraChannelName)
                if not guildPlayerRole:
                    await extraChannel.set_permissions(everyoneRole, view_channel=False)


            # Night channels
            for c in nightCat.channels:
                if c.type == discord.ChannelType.voice and c.name == nightChannelName:
                    neededNightChannels = neededNightChannels - 1

            if neededNightChannels > 0:
                for x in range(neededNightChannels):
                    await nightCat.create_voice_channel(nightChannelName)


            # Calling !addTown
            (post, info) = await self.resolveTownInfo(ctx, moverChannelName, townSquareChannelName, dayCatName, nightCatName, gameStRoleName, gameVillagerRoleName)

            if not post:
                await ctx.send("There was a problem creating the town of **" + townName + "**.")
                return
                
            await ctx.send("The town of **" + townName + "** has been created!")
            await self.addTownInternal(ctx, post, info, message_if_exists=False)

        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    
    @commands.command(name='destroyTown', aliases=['destroytown'], help='Destroy everything created from the \'createTown\' command')
    async def destroyTown(self, ctx):
        params = shlex.split(ctx.message.content)

        guild = ctx.guild
        
        usageStr = "Usage: `<town name>`"

        if len(params) < 1:
            await ctx.send("Too few params to `!destroyTown`. " + usageStr)
            return None

        townName = params[1]        
        if not townName:
            await ctx.send("No town name provided. " + usageStr)
            return None
        
        # These are in sync with those in createTown, could probably stand to abstract somehow
        dayCatName = townName
        nightCatName = townName + " - Night"
        gameStRoleName = townName + " Storyteller"
        gameVillagerRoleName = townName + " Villager"
        moverChannelName = "botc_mover"
        chatChannelName = "chat"
        townSquareChannelName = "Town Square"
        extraChannelNames = ["Dark Alley", "Library", "Graveyard"]
        nightChannelName = "Cottage"

        try:
            notDeleted = []
            notDeletedUnfamiliar = []
            success = True
                

            # Night category
            nightCat = getCategoryByName(guild, nightCatName)
            if nightCat:
                channelsToDestroy = []
                for c in nightCat.channels:
                    if c.type == discord.ChannelType.voice and c.name == nightChannelName:
                        channelsToDestroy.append(c)
                
                for c in channelsToDestroy:
                    #print("I want to delete: " + c.name)
                    await c.delete()
                    
                if len(nightCat.channels) == 0:
                    #print("I want to delete: " + nightCat.name)
                    await nightCat.delete()
                else:
                    for c in nightCat.channels:
                        notDeletedUnfamiliar.append("**" + nightCat.name + "** / **" + c.name + "** (channel)")
                    notDeleted.append("**" + nightCat.name + "** (category)")
                    success = False


            # Day category
            dayCat = getCategoryByName(guild, dayCatName)
            if dayCat:
                chatChannel = getChannelFromCategoryByName(dayCat, chatChannelName)
                if chatChannel and chatChannel.type == discord.ChannelType.text:
                    #print("I want to delete: " + chatChannel.name)
                    await chatChannel.delete()
                    
                townSquareChannel = getChannelFromCategoryByName(dayCat, townSquareChannelName)
                if townSquareChannel and townSquareChannel.type == discord.ChannelType.voice:
                    #print("I want to delete: " + townSquareChannel.name)
                    await townSquareChannel.delete()

                for extraChannelName in extraChannelNames:
                    extraChannel = getChannelFromCategoryByName(dayCat, extraChannelName)
                    if extraChannel and extraChannel.type == discord.ChannelType.voice:
                        #print("I want to delete: " + extraChannel.name)
                        await extraChannel.delete()

                moverChannel = getChannelFromCategoryByName(dayCat, moverChannelName)
                if moverChannel and moverChannel.type == discord.ChannelType.text:
                    #print("I want to delete: " + moverChannel.name)
                    # Do not delete the mover channel if there's a problem - you might
                    # still need it to send this very command!
                    if success and len(dayCat.channels) == 1:
                        await moverChannel.delete()
                    else:
                        notDeleted.append("**" + dayCat.name + "** / **" + moverChannel.name + "** (channel)")
                        
                if len(dayCat.channels) == 0:
                    #print("I want to delete: " + dayCat.name)
                    await dayCat.delete()
                else:
                    for c in dayCat.channels:
                        if c != moverChannel:
                            notDeletedUnfamiliar.append("**" + dayCat.name + "** / **" + c.name + "** (channel)")
                    notDeleted.append("**" + dayCat.name + "** (category)")
                    success = False


            # We want to leave the roles in place if there were any problems - the roles may be needed to see the channels
            # that need cleanup!
            gameVillagerRole = getRoleByName(guild, gameVillagerRoleName)
            if gameVillagerRole:
                if success:
                    #print("I want to delete: " + gameVillagerRole.name)
                    await gameVillagerRole.delete()
                else:
                    notDeleted.append("**" + gameVillagerRole.name + "** (role)")
                
            gameStRole = getRoleByName(guild, gameStRoleName)
            if gameStRole:
                if success:
                    #print("I want to delete: " + gameStRole.name)
                    await gameStRole.delete()
                else:
                    notDeleted.append("**" + gameStRole.name + "** (role)")

            
            # Figure out a useful message to send to the user
            message = "Town **" + townName + "** has been destroyed."
            if not success:
                message = "I destroyed what I knew about for Town **" + townName + "**." 
                if len(notDeletedUnfamiliar) > 0:
                    message =  message + "\n\nI did not destroy some things I was unfamiliar with:"
                    for s in notDeletedUnfamiliar:
                        message = message + "\n * " + s
                    message = message + "\n\nYou can destroy them and run this command again.\n"
                if len(notDeleted) > 0:
                    message =  message + "\nI did not destroy these things yet, just in case you still need them:"
                    for s in notDeleted:
                        message = message + "\n * " + s


            # Remove the town from the guild
            if success:
                post = {"guild" : guild.id, "dayCategory" : dayCatName}
                g_dbGuildInfo.delete_one(post)

            # Try to send to context, but it may have been a channel we deleted in which case send diretly to the author instead
            try:
                await ctx.send(message)
            except Exception as ex:
                await ctx.author.send(message)

            
        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)


    async def sendEmbed(self, ctx, townInfo):
        guild = ctx.guild
        embed = discord.Embed(title=f'{guild.name} // {townInfo.dayCategory.name}', description=f'Created {townInfo.timestamp} by {townInfo.authorName}', color=0xcc0000)
        embed.add_field(name="Control Channel", value=townInfo.controlChannel.name, inline=False)
        embed.add_field(name="Town Square", value=townInfo.townSquare.name, inline=False)
        embed.add_field(name="Day Category", value=townInfo.dayCategory.name, inline=False)
        embed.add_field(name="Night Category", value=townInfo.nightCategory.name, inline=False)
        embed.add_field(name="Storyteller Role", value=townInfo.storyTellerRole.name, inline=False)
        embed.add_field(name="Villager Role", value=townInfo.villagerRole.name, inline=False)
        await ctx.send(embed=embed)


class Gameplay(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

        # Start the timer if we have active games
        if g_dbActiveGames.count_documents({}) > 0:
            self.cleanupInactiveGames.start() 

    def cog_unload(self):
        self.cleanupInactiveGames.cancel()

    # Given a list of users, return a list of their names
    def userNames(self, users):
        return list(map(lambda x: x.display_name, users))

    # Helper to see if a command context is in a valid channel etc - used by all the commands
    async def isValid(self, ctx):

        if isinstance(ctx.channel, discord.DMChannel):
            await ctx.send(f"Whoops, you probably meant to send that in a text channel instead of a DM! Sorry, mate.")
            return False
        
        query = { "guild" : ctx.guild.id }
        result = g_dbGuildInfo.find(query)
        chanIds = map(lambda x: x["controlChannelId"], result)

        if isinstance(ctx.channel, discord.TextChannel):
            if ctx.channel.id in chanIds:
                return True

        return False

    async def onEndGameInternal(self, guild, info):
        # find all guild members with the Current Game role
        prevPlayers = set()
        prevSts = set()
        for m in guild.members:
            if info.villagerRole in m.roles:
                prevPlayers.add(m)
            if info.storyTellerRole in m.roles:
                prevSts.add(m)

        # remove game role from players
        for m in prevPlayers:
            await m.remove_roles(info.villagerRole)

        # remove cottage permissions
        for c in info.nightChannels:
            # Take away permission overwrites for this cottage
            for m in prevPlayers:
                await c.set_permissions(m, overwrite=None)
            for prevSt in prevSts:
                await c.set_permissions(prevSt, overwrite=None)

        for prevSt in prevSts:
            # remove storyteller role and name from storyteller
            await prevSt.remove_roles(info.storyTellerRole)
            if prevSt.display_name.startswith('(ST) '):
                newnick = prevSt.display_name[5:]
                try:
                    await prevSt.edit(nick=newnick)
                except:
                    pass


    # End the game and remove all the roles, permissions, etc
    @commands.command(name='endGame', aliases=['endgame'], help='End the current game and reset all permissions, roles, names, etc.')
    async def onEndGame(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            guild = ctx.guild

            info = self.bot.getTownInfo(ctx)

            await self.onEndGameInternal(guild, info)

            self.removeActiveGame(guild, ctx.channel)

        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    # Set the current storytellers
    @commands.command(name='setStorytellers', aliases=['setstorytellers', 'setStoryTellers', 'storytellers', 'storyTellers', 'setsts', 'setSts', 'setSTs', 'setST', 'setSt', 'setst', 'sts', 'STs', 'Sts'], help='Set a list of users to be Storytellers.')
    async def onSetSTs(self, ctx):
        if not await self.isValid(ctx):
            return

        info = self.bot.getTownInfo(ctx)

        names = shlex.split(ctx.message.content)
        
        sts = list(map(lambda x: self.getClosestUser(info.activePlayers, x), names[1:]))

        await self.setStorytellersInternal(ctx, sts)
        


    # Helper for setting a list of users as the current storytellers
    async def setStorytellersInternal(self, ctx, sts):

        info = self.bot.getTownInfo(ctx)

        # take any (ST) off of old storytellers
        for o in info.storyTellers:
            if o not in sts:
                await o.remove_roles(info.storyTellerRole)
            if o not in sts and o.display_name.startswith('(ST) '):
                newnick = o.display_name[5:]
                try:
                    await o.edit(nick=newnick)
                except:
                    pass

        # set up the new storytellers
        for storyTeller in sts:
            if storyTeller is None:
                continue

            await storyTeller.add_roles(info.storyTellerRole)
            
            # add (ST) to the start of the current storyteller
            if not storyTeller.display_name.startswith('(ST) '):
                try:
                    await storyTeller.edit(nick=f"(ST) {storyTeller.display_name}")
                except:
                    pass

    # Set the players in the normal voice channels to have the 'Current Game' role, granting them access to whatever that entails
    @commands.command(name='currGame', aliases=['currgame', 'curgame', 'curGame'], help='Set the current users in all standard BotC voice channels as players in a current game, granting them roles to see channels associated with the game.')
    async def onCurrGame(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            guild = ctx.guild

            info = self.bot.getTownInfo(ctx)

            # find all guild members with the Current Game role
            prevPlayers = set()
            for m in guild.members:
                if info.villagerRole in m.roles:
                    prevPlayers.add(m)

            # grant the storyteller the Current Storyteller role if necessary
            storyTeller = ctx.message.author
            if storyTeller not in info.storyTellers:
                await self.setStorytellersInternal(ctx, [storyTeller])

            # find additions and deletions by diffing the sets
            remove = prevPlayers - info.activePlayers
            add = info.activePlayers - prevPlayers

            # remove any stale players
            if len(remove) > 0:
                removeMsg = f"Removed {info.villagerRole.name} role from: "
                removeMsg += ', '.join(self.userNames(remove))
                for m in remove:
                    await m.remove_roles(info.villagerRole)
                await ctx.send(removeMsg)

            # add any new players
            if len(add) > 0:
                addMsg = f"Added {info.villagerRole.name} role to: "
                addMsg += ', '.join(self.userNames(add))
                for m in add:
                    await m.add_roles(info.villagerRole)
                await ctx.send(addMsg)

            self.recordGameActivity(guild, ctx.channel)

            # Set a timer to clean up active games eventually
            if not self.cleanupInactiveGames.is_running():
                self.cleanupInactiveGames.start()

        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    # Given a list of users and a name string, find the user with the closest name
    def getClosestUser(self, userlist, name):
        for u in userlist:
            # See if anybody's name starts with what was sent
            uname = u.display_name.lower()
            # But ignore the starting (ST) for storytellers
            if uname.startswith('(st) '):
                uname = uname[5:]

            if uname.startswith(name.lower()):
                return u
        
        return None

    # Common code for parsing !evil and !lunatic commands
    async def processMinionMessage(self, ctx, users):

        # Split the message allowing quoted substrings
        params = shlex.split(ctx.message.content)
        # Delete the input message
        await ctx.message.delete()

        # Grab the demon and list of minions
        demon = params[1]
        minions = params[2:]

        if len(minions) == 0:
            await self.bot.sendErrorToAuthor(ctx, f"It seems you forgot to specify any minions!")
            return (False, None, None)

        # Get the users from the names
        demonUser = self.getClosestUser(users, demon)
        minionUsers = list(map(lambda x: self.getClosestUser(users, x), minions))

        info = self.bot.getTownInfo(ctx)
        categories = [info.dayCategory.name, info.nightCategory.name]
        catString = ', '.join(categories)

        # Error messages for users not found
        if demonUser is None:
            await self.bot.sendErrorToAuthor(ctx, f"Couldn't find user **{demon}** in these categories: {catString}.")
            return (False, None, None)

        for (i, m) in enumerate(minionUsers):
            if m is None:
                await self.bot.sendErrorToAuthor(ctx, f"Couldn't find user **{minions[i]}** in these categories: {catString}.")
                return (False, None, None)

        return (True, demonUser, minionUsers)

    # Send a message to the demon
    async def sendDemonMessage(self, demonUser, minionUsers):
        demonMsg = f"{demonUser.display_name}: You are the **demon**. Your minions are: "
        minionNames = self.userNames(minionUsers)
        demonMsg += ', '.join(minionNames)
        await demonUser.send(demonMsg)

    # Command to send fake evil info to the Lunatic
    # Works the same as !evil, but doesn't message the minions
    @commands.command(name='lunatic', help='Send fake evil info to the Lunatic. Format is `!lunatic <Lunatic> <fake minion> <fake minion> <fake minion>`')
    async def onLunatic(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            info = self.bot.getTownInfo(ctx)
            users = info.activePlayers
            (success, demonUser, minionUsers) = await self.processMinionMessage(ctx, users)

            if not success:
                return

            await self.sendDemonMessage(demonUser, minionUsers)

        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    # Command to send demon/minion info to the Demon and Minions
    @commands.command(name='evil', help='Send evil info to evil team. Format is `!evil <demon> <minion> <minion> <minion>`')
    async def onEvil(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            info = self.bot.getTownInfo(ctx)
            users = info.activePlayers
            (success, demonUser, minionUsers) = await self.processMinionMessage(ctx, users)

            if not success:
                return

            await self.sendDemonMessage(demonUser, minionUsers)

            minionMsg = "{}: You are a **minion**. Your demon is: {}."
            
            if len(minionUsers) > 1:
                minionMsg += " Your fellow minions are: {}."

            for m in minionUsers:
                otherMinions = self.userNames(minionUsers)
                otherMinions.remove(m.display_name)

                otherMinionsMsg = ', '.join(otherMinions)
                formattedMsg = minionMsg.format(m.display_name, demonUser.display_name, otherMinionsMsg)
                await m.send(formattedMsg)

            await ctx.send("The Evil team has been informed...")
                
        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    # Move users to the night cottages
    @commands.command(name='night', help='Move users to Cottages in the BotC - Nighttime category')
    async def onNight(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            # do role switching for active game first!
            await self.onCurrGame(ctx)

            # get channels we care about
            info = self.bot.getTownInfo(ctx)

            # get list of users in town square   
            users = list(info.activePlayers)
            users.sort(key=lambda x: x.display_name)
            cottages = list(info.nightChannels)
            cottages.sort(key=lambda x: x.position)

            await ctx.send(f'Moving {len(users)} users to Cottages!')
            
            # pair up users with cottages
            pairs = list(map(lambda x, y: (x,y), users, cottages))
            # randomize the order people are moved
            random.shuffle(pairs)

            # move each user to a cottage
            for (user, cottage) in pairs:
                # grant the user permissions for their own cottage so they can see streams (if they're the Spy, for example)
                await cottage.set_permissions(user, view_channel=True)
                await user.move_to(cottage)

        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    # Move users from night Cottages back to Town Square
    @commands.command(name='day', help='Move users from Cottages back to Town Square')
    async def onDay(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            info = self.bot.getTownInfo(ctx)

            # get users in night channels
            users = list()
            for c in info.nightChannels:
                users.extend(c.members)
                # Take away permission overwrites for their cottage
                for m in c.members:
                    await c.set_permissions(m, overwrite=None)

            await ctx.send(f'Moving {len(users)} users from Cottages to {info.townSquare.name}.')

            # randomize the order we bring people back
            random.shuffle(users)

            # move them to Town Square
            for user in users:
                await user.move_to(info.townSquare)

        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    # Move users from other daytime channels back to Town Square
    @commands.command(name='vote', help='Move users from daytime channels back to Town Square')
    async def onVote(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            info = self.bot.getTownInfo(ctx)
            
            # get users in day channels other than Town Square
            users = list()
            for c in info.dayChannels:
                if c != info.townSquare:
                    users.extend(c.members)

            await ctx.send(f'Moving {len(users)} users from daytime channels to {info.townSquare.name}.')

            # move them to Town Square
            for user in users:
                await user.move_to(info.townSquare)
        
        except Exception as ex:
            await self.bot.sendErrorToAuthor(ctx)

    def recordGameActivity(self, guild, controlChan):
        post = { "guild" : guild.id, "channel" : controlChan.id, "lastActivity" : datetime.datetime.now() }
        query = { "guild" : guild.id, "channel" : controlChan.id }
        # Upsert this record
        g_dbActiveGames.replace_one(query, post, True)

    def removeActiveGame(self, guild, controlChan):
        # Delete this game's record
        g_dbActiveGames.delete_one( {"guild" : guild.id, "channel": controlChan.id })

    # Called periodically to try and clean up old games that aren't in progress anymore
    # This is 8 hours, with inactivity at 3, as the expectation is that 3-4 hours is probably
    # at the the common upper limit for a town to be active, so 5 hours should catch most games.
    # For debugging, use the 3-second loop and the 10-second delta below
    #@tasks.loop(seconds=3)
    @tasks.loop(hours=8)
    async def cleanupInactiveGames(self):
        print("Checking for inactive games")
        # Inactive games are ones that haven't had any activity in the last 3 hours
        inactiveTime = datetime.datetime.now() - datetime.timedelta(hours=3)
        #inactiveTime = datetime.datetime.now() - datetime.timedelta(seconds=10)

        numEnded = 0

        # "Range query" that gives us all documents with a timestamp less than our inactive time
        inactiveQuery = {"lastActivity" : {"$lt": inactiveTime}}
        
        for rec in g_dbActiveGames.find(inactiveQuery):
            guildId = rec["guild"]
            guild = self.bot.get_guild(guildId)
            # Fetch the full info for this guild/channel combo
            lookupQuery = { "guild" : guildId, "controlChannelId" : rec["channel"] }
            doc = g_dbGuildInfo.find_one(lookupQuery)
            if doc:
                info = TownInfo(guild, doc)
                try:
                    await self.onEndGameInternal(guild, info)
                    numEnded = numEnded + 1
                except Exception:
                    pass

        if numEnded > 0:
            print(f'Ended {numEnded} inactive game(s)')

        # Remove all the inactive towns from our list
        result = g_dbActiveGames.delete_many(inactiveQuery)

        # If nothing is left active at all, stop the timer
        if g_dbActiveGames.count_documents({}) == 0:
            print("No remaining active games, stopping checks")
            self.cleanupInactiveGames.stop()
    
    @cleanupInactiveGames.before_loop
    async def beforecleanupInactiveGames(self):
        await self.bot.wait_until_ready()


COMMAND_PREFIX = os.getenv('COMMAND_PREFIX') or '!'
bot = botcBot(command_prefix=COMMAND_PREFIX, intents=intents, description='Bot to manage playing Blood on the Clocktower via Discord')
bot.add_cog(Setup(bot))
bot.add_cog(Gameplay(bot))
bot.run(TOKEN)
