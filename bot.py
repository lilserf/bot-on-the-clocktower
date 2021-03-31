import os

import discord
import json
from dotenv import load_dotenv
from discord.ext import commands
from operator import itemgetter, attrgetter
import shlex
import traceback
import random
import pymongo
from pymongo import MongoClient

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
MONGO_CONNECT = os.getenv('MONGO_CONNECT')
if MONGO_CONNECT is None:
    raise Exception("No MONGO_CONNECT string found. Be sure you have MONGO_CONNECT defined in your environment")
cluster = MongoClient(MONGO_CONNECT)
db = cluster['botc']

guildInfo = db['GuildInfo']

# Helpers
def getChannelFromCategoryByName(category, name):
    return discord.utils.find(lambda c: (c.type == discord.ChannelType.voice or c.type == discord.ChannelType.text) and c.name == name, category.channels)

def getCategoryByName(guild, name):
    return discord.utils.find(lambda c: c.type == discord.ChannelType.category and c.name == name, guild.channels)

def getRoleByName(guild, name):
    return discord.utils.find(lambda r: r.name==name, guild.roles)

# Nicer than a dict for storing info about the town
class TownInfo:
    def __init__(self, ctx, document):
        guild = ctx.guild

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

            self.authorName = document["authorName"]
            self.timestamp = document["timestamp"]

# Bot subclass
class botcBot(commands.Bot):

    # Get a well-defined TownInfo based on the stored DB info for this guild
    def getTownInfo(self, ctx):

        query = { "guild" : ctx.guild.id, "controlChannelId" : ctx.channel.id }
        doc = guildInfo.find_one(query)

        if doc:
            return TownInfo(ctx, doc)
        else:
            return None

    async def on_ready(self):
        print(f'{bot.user.name} has connected to Discord!')

# Setup cog
class Setup(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        
    # Helper to send a message to the author of the command about what they did wrong
    async def sendErrorToAuthor(self, ctx, error=None):
        if error is not None:
            formatted = error
        else:
            formatted = '```\n' + traceback.format_exc(3) + '\n```'
            traceback.print_exc()
        await ctx.author.send(f"Alas, an error has occurred:\n{formatted}\n(from message `{ctx.message.content}`)")


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
            existing = guildInfo.find_one(query)
            if existing:
                await ctx.send(f'Found an existing town on this server using daytime category `{post["dayCategory"]}`, modifying it!')

        # Upsert the town into place
        #print(f'Adding a town to guild {post["guild"]} with control channel [{post["controlChannel"]}], day category [{post["dayCategory"]}], night category [{post["nightCategory"]}]')
        guildInfo.replace_one(query, post, True)

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
        guildInfo.delete_one(post)

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

        if len(params) < 1:
            await ctx.send("Too few params to `!createTown`. " + usageStr)
            return None

        townName = params[1]        
        if not townName:
            await ctx.send("No town name provided. " + usageStr)
            return None
            
        botRole = getRoleByName(guild, self.bot.user.name)
        if not botRole:
            await ctx.send("Could not find role for \"" + self.bot.user.name + "\". Cannot proceed! Where did the role go?")
            return None
        
        serverStRole = len(params) >= 2 and getRoleByName(guild, params[2]) or None
        serverPlayerRole = len(params) >= 3 and getRoleByName(guild, params[3]) or None
        
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
            # Roles
            everyoneRole = getRoleByName(guild, "@everyone")
            if not everyoneRole:
                await ctx.send("Could not find the \"@everyone\" role. Why not?")
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
            
            if serverStRole:
                await moverChannel.set_permissions(serverStRole, view_channel=True)
                await moverChannel.set_permissions(everyoneRole, view_channel=False)


            # Chat channel
            chatChannel = getChannelFromCategoryByName(dayCat, chatChannelName)
            if not chatChannel:
                chatChannel = await dayCat.create_text_channel(chatChannelName)


            # Town Square 
            townSquareChannel = getChannelFromCategoryByName(dayCat, townSquareChannelName)
            if not townSquareChannel:
                townSquareChannel = await dayCat.create_voice_channel(townSquareChannelName)

            if serverPlayerRole:
                await dayCat.set_permissions(everyoneRole, view_channel=False)
                await townSquareChannel.set_permissions(serverPlayerRole, view_channel=True)
            
            
            # Extra day channels
            for extraChannelName in extraChannelNames:
                extraChannel = getChannelFromCategoryByName(dayCat, extraChannelName)
                if not extraChannel:
                    extraChannel = await dayCat.create_voice_channel(extraChannelName)


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
                await ctx.send("There was a problem creating the town of \"" + townName + "\".")
                return
                
            await ctx.send("The town of \"" + townName + "\" has been created!")
            await self.addTownInternal(ctx, post, info, message_if_exists=False)

        except Exception as ex:
            await self.sendErrorToAuthor(ctx)


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

    # Given a list of users, return a list of their names
    def userNames(self, users):
        return list(map(lambda x: x.display_name, users))

    # Helper to see if a command context is in a valid channel etc - used by all the commands
    async def isValid(self, ctx):

        if isinstance(ctx.channel, discord.DMChannel):
            await ctx.send(f"Whoops, you probably meant to send that in a text channel instead of a DM! Sorry, mate.")
            return False
        
        query = { "guild" : ctx.guild.id }
        result = guildInfo.find(query)
        chanIds = map(lambda x: x["controlChannelId"], result)

        if isinstance(ctx.channel, discord.TextChannel):
            if ctx.channel.id in chanIds:
                return True

        return False

    # End the game and remove all the roles, permissions, etc
    @commands.command(name='endGame', aliases=['endgame'], help='End the current game and reset all permissions, roles, names, etc.')
    async def onEndGame(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            guild = ctx.guild

            info = self.bot.getTownInfo(ctx)

            # find all guild members with the Current Game role
            prevPlayers = set()
            prevSt = None
            for m in guild.members:
                if info.villagerRole in m.roles:
                    prevPlayers.add(m)
                if info.storyTellerRole in m.roles:
                    prevSt = m

            # remove game role from players
            for m in prevPlayers:
                await m.remove_roles(info.villagerRole)

            # remove cottage permissions
            for c in info.nightChannels:
                # Take away permission overwrites for this cottage
                for m in prevPlayers:
                    await c.set_permissions(m, overwrite=None)
                if prevSt:
                    await c.set_permissions(prevSt, overwrite=None)

            if prevSt:
                # remove storyteller role and name from storyteller
                await prevSt.remove_roles(info.storyTellerRole)
                if prevSt.display_name.startswith('(ST) '):
                    newnick = prevSt.display_name[5:]
                    await prevSt.edit(nick=newnick)

        except Exception as ex:
            await self.sendErrorToAuthor(ctx)


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

            # grant the storyteller the Current Storyteller role
            storyTeller = ctx.message.author
            await storyTeller.add_roles(info.storyTellerRole)

            # take any (ST) off of old storytellers
            for o in info.activePlayers:
                if o != storyTeller and info.storyTellerRole in o.roles:
                    await o.remove_roles(info.storyTellerRole)
                if o != storyTeller and o.display_name.startswith('(ST) '):
                    newnick = o.display_name[5:]
                    await o.edit(nick=newnick)
            
            # add (ST) to the start of the current storyteller
            if not storyTeller.display_name.startswith('(ST) '):
                await storyTeller.edit(nick=f"(ST) {storyTeller.display_name}")

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

        except Exception as ex:
            await self.sendErrorToAuthor(ctx)

    # Given a list of users and a name string, find the user with the closest name
    def getClosestUser(self, userlist, name):
        for u in userlist:
            # See if anybody's name starts with what was sent
            if u.display_name.lower().startswith(name.lower()):
                return u
        
        return None

    # Helper to send a message to the author of the command about what they did wrong
    async def sendErrorToAuthor(self, ctx, error=None):
        if error is not None:
            formatted = error
        else:
            formatted = '```\n' + traceback.format_exc(3) + '\n```'
            traceback.print_exc()
        await ctx.author.send(f"Alas, an error has occurred:\n{formatted}\n(from message `{ctx.message.content}`)")

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
            await self.sendErrorToAuthor(ctx, f"It seems you forgot to specify any minions!")
            return (False, None, None)

        # Get the users from the names
        demonUser = self.getClosestUser(users, demon)
        minionUsers = list(map(lambda x: self.getClosestUser(users, x), minions))

        info = self.bot.getTownInfo(ctx)
        categories = [info.dayCategory.name, info.nightCategory.name]
        catString = ', '.join(categories)

        # Error messages for users not found
        if demonUser is None:
            await self.sendErrorToAuthor(ctx, f"Couldn't find user **{demon}** in these categories: {catString}.")
            return (False, None, None)

        for (i, m) in enumerate(minionUsers):
            if m is None:
                await self.sendErrorToAuthor(ctx, f"Couldn't find user **{minions[i]}** in these categories: {catString}.")
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
            await self.sendErrorToAuthor(ctx)

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
            await self.sendErrorToAuthor(ctx)

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
            await self.sendErrorToAuthor(ctx)

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
            await self.sendErrorToAuthor(ctx)

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
            await self.sendErrorToAuthor(ctx)

COMMAND_PREFIX = os.getenv('COMMAND_PREFIX') or '!'
bot = botcBot(command_prefix=COMMAND_PREFIX, intents=intents, description='Bot to manage playing Blood on the Clocktower via Discord')
bot.add_cog(Setup(bot))
bot.add_cog(Gameplay(bot))
bot.run(TOKEN)
