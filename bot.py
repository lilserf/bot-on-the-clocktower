import os

from dotenv import load_dotenv
import botctypes
from botctypes import TownInfo
import datetime
import discord
import discordhelper
from discord.ext import commands, tasks
import json
from operator import itemgetter, attrgetter
import pymongo
from pymongo import MongoClient
import random
import shlex
import traceback

import votetimer
import announce
import lookup
import gameplay

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
MONGO_DB = os.getenv('MONGO_DB') or 'botc'
if MONGO_CONNECT is None:
    raise Exception("No MONGO_CONNECT string found. Be sure you have MONGO_CONNECT defined in your environment")
cluster = MongoClient(MONGO_CONNECT)
db = cluster[MONGO_DB]

COMMAND_PREFIX = os.getenv('COMMAND_PREFIX') or '!'

g_dbGuildInfo = db['GuildInfo']
g_dbActiveGames = db['ActiveGames']

# Do some sanity checking of a DB document and see if a valid town can even be found on a guild with these params
def isTownValid(guild, doc):
    dayCat = discordhelper.get_category(guild, doc["dayCategory"], doc["dayCategoryId"])
    if dayCat is None:
        return (False, "missing day category " + doc["dayCategory"])

    townSquare = discordhelper.get_channel_from_category(dayCat, doc["townSquare"], doc["townSquareId"])
    if townSquare is None:
        return (False, "missing Town Square " + doc["townSquare"])

    control = discordhelper.get_channel_from_category(dayCat, doc["controlChannel"], doc["controlChannelId"])
    if control is None:
        return (False, "missing control channel " + doc["controlChannel"])

    stRole = discordhelper.get_role(guild, doc["storyTellerRole"], doc["storyTellerRoleId"])
    if stRole is None:
        return (False, "missing Storyteller role " + doc["storyTellerRole"])

    villagerRole = discordhelper.get_role(guild, doc["villagerRole"], doc["villagerRoleId"])
    if villagerRole is None:
        return (False, "missing Villager role " + doc["villagerRole"])

    return (True, "Valid")


# Bot subclass
class botcBot(commands.Bot):

    # Get a well-defined TownInfo based on the stored DB info for this guild
    def getTownInfo(self, ctx):
        return self.getTownInfoByIds(ctx.guild.id, ctx.channel.id, ctx.guild)

    def getTownInfoByTownId(self, town_id, guild=None):
        return self.getTownInfoByIds(town_id.guild_id, town_id.channel_id, guild)

    def getTownInfoByIds(self, guild_id, channel_id, guild=None):
        query = { "guild" : guild_id, "controlChannelId" : channel_id }
        doc = g_dbGuildInfo.find_one(query)
        
        if not guild:
            guild = self.get_guild(guild_id)

        if doc and guild:
            return TownInfo(guild, doc)
        else:
            return None
    def get_announce_cog(self):
        return self.get_cog('Version Announcements')

    async def on_ready(self):
        print(f'{self.user.name} has connected to Discord! Command prefix: {COMMAND_PREFIX}')
        print('Sending version announcements...')
        announcer = self.get_announce_cog()
        num_sent = await announcer.announce_latest_version()
        print(f'Sent announcements to {num_sent} towns.')

# Announcer cog
class AnnouncerCog(commands.Cog, name='Version Announcements'):
    def __init__(self, bot):
        self.bot = bot;
        self.announcer = announce.Announcer(bot, db)

    async def announce_latest_version(self):
        return await self.announcer.announce_latest_version()
    
    def set_to_latest_version(self, guild_id):
        self.announcer.set_to_latest_version(guild_id)

    @commands.command(name='announce', help='Opt into new version announcement messages')
    async def optIn(self, ctx):
        await self.perform_action_reporting_errors(self.optInInternal, ctx)

    @commands.command(name='noannounce', help='Opt out of new version announcement messages')
    async def optOut(self, ctx):
        await self.perform_action_reporting_errors(self.optOutInternal, ctx)

    async def optInInternal(self, ctx):
        self.announcer.guild_yes_announce(ctx.guild.id)
        return 'This server will now receive new version announcement messages.'

    async def optOutInternal(self, ctx):
        self.announcer.guild_no_announce(ctx.guild.id)
        return 'This server should no longer receive new version announcement messages.'

    async def perform_action_reporting_errors(self, action, ctx):
        try:
            message = await action(ctx)
            if message != None:
                await ctx.send(message)

        except Exception as ex:
            await discordutil.send_error_to_author(ctx)


# Setup cog
class SetupCog(commands.Cog, name='Setup'):
    def __init__(self, bot):
        self.bot = bot

    def setToLatestVersion(self, guild):
        self.bot.get_announce_cog().set_to_latest_version(guild)

    @commands.command(name='townInfo', aliases=['towninfo'], help='Show the stored info about the channels and roles that make up this town')
    async def townInfo(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            info = self.bot.getTownInfo(ctx)

            if info is not None:
                await self.sendEmbed(ctx, info)
            else:
                await ctx.send("Sorry, I couldn't find a town registered to this channel.")
    
    
    async def addTownInternal(self, ctx, post, info, message_if_exists=True):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            guild = ctx.guild
            
            self.setToLatestVersion(guild.id)

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
            nightCatInfo = post["nightCategory"] and f'night category [{post["nightCategory"]}]' or '<no night category>'
            print(f'Adding a town to guild {post["guild"]} with control channel [{post["controlChannel"]}], day category [{post["dayCategory"]}], {nightCatInfo}')
            g_dbGuildInfo.replace_one(query, post, True)

            await self.sendEmbed(ctx, info)

    @commands.command(name='addTown', aliases=['addtown'], help=f'Add a game on this server.\n\nUsage: {COMMAND_PREFIX}addTown <control channel> <town square channel> <day category> <night category> <storyteller role> <villager role> [chat channel]\n\nAlternate usage: {COMMAND_PREFIX}addTown control=<control channel> townSquare=<town square channel> dayCategory=<day category> [nightCategory=<night category>] stRole=<storyteller role> villagerRole=<villager role> [chatChannel=<chat channel>]')
    async def addTown(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            params = shlex.split(ctx.message.content)

            (post, info) = await self.resolveTownInfoParams(ctx, params)

            if not post:
                return

            await self.addTownInternal(ctx, post, info)


    @commands.command(name='removeTown', aliases=['removetown'], help='Remove a game on this server')
    async def removeTown(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            guild = ctx.guild

            params = shlex.split(ctx.message.content)
            usageStr = f'Usage: `{COMMAND_PREFIX}removeTown <day category name>` or `{COMMAND_PREFIX}removeTown` alone if run from the town\'s control channel'

            if len(params) == 1:
                post = {"guild" : guild.id, "controlChannelId" : ctx.channel.id }
                print(f'Removing a game from guild {post["guild"]} with control channel [{post["controlChannelId"]}]')
            elif len(params) == 2:
                post = {"guild" : guild.id, "dayCategory" : params[1]}
                print(f'Removing a game from guild {post["guild"]} with day category [{post["dayCategory"]}]')
            else:
                await ctx.send(f'Unexpected parameters. {usageStr}')
                return

            info = g_dbGuildInfo.find_one(post)
            result = g_dbGuildInfo.delete_one(post)

            if result.deleted_count > 0:
                embed = discord.Embed(title=f'{guild.name} // {info["dayCategory"]}', description=f'This town is no longer registered.', color=0xcc0000)
                await ctx.send(embed=embed)
            else:
                await ctx.send(f"Couldn't find a town to remove! {usageStr}")

    @commands.command(name='setChatChannel', help=f'Set the chat channel associated with this town.\n\nUsage: {COMMAND_PREFIX}setChatChannel <chat channel>')
    async def set_chat_channel(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            params = shlex.split(ctx.message.content)
        
            if len(params) != 2:
                await ctx.send(f'Incorrect usage for `{COMMAND_PREFIX}setChatChannel`: should provide `<chat channel>`')
                return None

            chat_channel_name = params[1]
        
            query = { "guild" : ctx.guild.id, "controlChannelId" : ctx.channel.id }
            doc = g_dbGuildInfo.find_one(query)
            if not doc:
                await ctx.send(f'Could not find a town! Are you running this command from the town control channel?')
                return None

            day_category = discordhelper.get_category(ctx.guild, doc['dayCategory'], doc['dayCategory'])
            if not day_category:
                await ctx.send(f'Could not find the category {doc["dayCategory"]}!')
                return None

            chat_channel = discordhelper.get_channel_from_category_by_name(day_category, chat_channel_name)
            if not chat_channel:
                await ctx.send(f'Could not find the channel {chat_channel_name} in the category {doc["dayCategory"]}!')
                return None

            doc['chatChannel'] = chat_channel.name
            doc['chatChannelId'] = chat_channel.id
            g_dbGuildInfo.replace_one(query, doc, True)

            info = self.bot.getTownInfo(ctx)
            if info:
                await self.sendEmbed(ctx, info)
            else:
                await ctx.send(f'There was a problem adding {chat_channel_name} to the town.')


    # Parse all the params for addTown, sanity check them, and return useful dicts
    async def resolveTownInfoParams(self, ctx, params):
        controlName = None
        townSquareName = None
        dayCatName = None
        nightCatName = None
        stRoleName = None
        villagerName = None
        chatChannelName = None

        hasNamedArgs = False

        for i in range(1, len(params)):
            ar = params[i].split("=")
            if len(ar) != 2:
                hasNamedArgs = False
                break

            hasNamedArgs = True
            p = ar[0].lower()
            v = ar[1]

            if p == "control":
                controlName = v
            elif p == "townsquare":
                townSquareName = v
            elif p == "daycategory":
                dayCatName = v
            elif p == "nightcategory":
                nightCatName = v
            elif p == "strole":
                stRoleName = v
            elif p == "villagerrole":
                villagerName = v
            elif p == "chatchannel":
                chatChannelName = v
            else:
                await ctx.send(f'Unknown param to `{COMMAND_PREFIX}addTown`: \"{p}\". Valid params: control, townSquare, dayCategory, nightCategory, stRole, villagerRole, chatChannel')
                return (None, None)

        if not hasNamedArgs:
            if len(params) < 7:
                await ctx.send(f'Too few params to `{COMMAND_PREFIX}addTown`: should provide `<control channel> <townsquare channel> <day category> <night category> <storyteller role> <villager role> [chat channel]`')
                return (None, None)

            controlName = params[1]
            townSquareName = params[2]
            dayCatName = params[3]
            nightCatName = params[4]
            stRoleName = params[5]
            villagerName = params[6]
            if len(params) > 7:
                chatChannelName = params[7]
        
        return await self.resolveTownInfo(ctx, controlName, townSquareName, dayCatName, nightCatName, stRoleName, villagerName, chatChannelName)


    # Using passed-in name params, resolve town info and find all the stuff needed to post to DB
    async def resolveTownInfo(self, ctx, controlName, townSquareName, dayCatName, nightCatName, stRoleName, villagerName, chatChannelName):
        guild = ctx.guild
        
        dayCat = discordhelper.get_category_by_name(guild, dayCatName)
        nightCat = nightCatName and discordhelper.get_category_by_name(guild, nightCatName) or None
        
        controlChan = discordhelper.get_channel_from_category_by_name(dayCat, controlName)
        townSquare = discordhelper.get_channel_from_category_by_name(dayCat, townSquareName)

        stRole = discordhelper.get_role_by_name(guild, stRoleName)
        villagerRole = discordhelper.get_role_by_name(guild, villagerName)

        chatChannel = None
        if chatChannelName:
            chatChannel = discordhelper.get_channel_from_category_by_name(dayCat, chatChannelName)

        # TODO: more checks
        # does the night category contain some channels to distribute people to?
        
        # TODO: also specify the roles to set
        # does the storyteller role have perms to see all the cottages?
        # do the player roles NOT have perms to see the cottages

        if not dayCat:
            await ctx.send(f'Couldn\'t find a category named `{dayCatName}`!')
            return None

        if not controlChan:
            await ctx.send(f'Couldn\'t find a channel named `{controlName}` in category `{dayCatName}`!')
            return None

        if chatChannelName and not chatChannel:
            await ctx.send(f'Couldn\'t find a chat channel named `{chatChannelName}` in category `{dayCatName}`!')
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

        # Some categories/channels are optional
        nightCatId = nightCat and nightCat.id or None
        chatChannelId = chatChannel and chatChannel.id or None

        # Object suitable for mongo
        post = {   
            "guild" : guild.id,
            "controlChannel" : controlName,
            "controlChannelId" : controlChan.id,
            "chatChannel" : chatChannelName,
            "chatChannelId" : chatChannelId,
            "townSquare" : townSquareName,
            "townSquareId" : townSquare.id,
            "dayCategory" : dayCatName,
            "dayCategoryId" : dayCat.id,
            "nightCategory" : nightCatName,
            "nightCategoryId" : nightCatId,
            "storyTellerRole" : stRoleName,
            "storyTellerRoleId" : stRole.id,
            "villagerRole" : villagerName,
            "villagerRoleId" : villagerRole.id,
            "authorName" : ctx.author.display_name,
            "author" : ctx.author.id,
            "timestamp" : ctx.message.created_at
        }

        objs = TownInfo(guild, post)

        return (post, objs)


    @commands.command(name='createTown', aliases=['createtown'], help=f'Create an entire town on this server, including categories, roles, channels, and permissions.\n\nUsage: {COMMAND_PREFIX}createTown <town name> [server storyteller role] [server player role] [noNight]')
    async def createTown(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            params = shlex.split(ctx.message.content)

            guild = ctx.guild
        
            usageStr = f"Usage: `{COMMAND_PREFIX}createTown <town name> [server storyteller role] [server player role] [noNight]`"

            if len(params) < 2:
                await ctx.send(f"Too few params to `{COMMAND_PREFIX}createTown`. " + usageStr)
                return None

            townName = params[1]
            if not townName:
                await ctx.send("No town name provided. " + usageStr)
                return None
        
            # Ensure all the roles exist
            botRole = discordhelper.get_role_by_name(guild, self.bot.user.name)
            if not botRole:
                await ctx.send("Could not find role for **" + self.bot.user.name + "**. Cannot proceed! Where did the role go?")
                return None
        
            allowNightCategory = True
            guildStRole = None
            guildPlayerRole = None

            # Check for additional params beyond the required ones
            additionalParamCount = 0

            for i in range(2, len(params)):
                p = params[i]
                if p.lower() == "nonight":
                    allowNightCategory = False
                else:
                    if additionalParamCount == 0:
                        guildStRole = discordhelper.get_role_by_name(guild, p)
                        if not guildStRole:
                            await ctx.send("Provided Storyteller Role **" + p + "** not found.")
                            return None
                    elif additionalParamCount == 1:
                        guildPlayerRole = discordhelper.get_role_by_name(guild, p)
                        if not guildPlayerRole:
                            await ctx.send("Provided Player Role **" + p + "** not found.")
                            return None
                    else:
                        await ctx.send(f"Unknown parameter: {p}")
                        return None

                    additionalParamCount = additionalParamCount + 1
        
            # These are in sync with those in destroyTown, could probably stand to abstract somehow
            dayCatName = townName
            nightCatName = allowNightCategory and townName + " - Night" or None
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
                everyoneRole = discordhelper.get_role_by_name(guild, "@everyone")
                if not everyoneRole:
                    await ctx.send("Could not find the **@everyone** role. Why not?")
                    return None
        
                gameVillagerRole = discordhelper.get_role_by_name(guild, gameVillagerRoleName)
                if not gameVillagerRole:
                    gameVillagerRole = await guild.create_role(name=gameVillagerRoleName, color=discord.Color.magenta())
                
                gameStRole = discordhelper.get_role_by_name(guild, gameStRoleName)
                if not gameStRole:
                    gameStRole = await guild.create_role(name=gameStRoleName, color=discord.Color.dark_magenta()) 


                # Day category
                dayCat = discordhelper.get_category_by_name(guild, dayCatName)
                if not dayCat:
                    dayCat = await guild.create_category(dayCatName)
            
                await dayCat.set_permissions(gameVillagerRole, view_channel=True)
                await dayCat.set_permissions(botRole, view_channel=True, move_members=True)


                # Night category
                if allowNightCategory:
                    nightCat = discordhelper.get_category_by_name(guild, nightCatName)
                    if not nightCat:
                        nightCat = await guild.create_category(nightCatName)

                    await nightCat.set_permissions(gameStRole, view_channel=True)
                    await nightCat.set_permissions(botRole, view_channel=True, move_members=True)
                    await nightCat.set_permissions(everyoneRole, view_channel=False)


                # Mover channel
                moverChannel = discordhelper.get_channel_from_category_by_name(dayCat, moverChannelName)
                if not moverChannel:
                    moverChannel = await dayCat.create_text_channel(moverChannelName)
                await moverChannel.set_permissions(botRole, view_channel=True)
                await moverChannel.set_permissions(gameVillagerRole, overwrite=None)
            
                if guildStRole:
                    await moverChannel.set_permissions(guildStRole, view_channel=True)
                    await moverChannel.set_permissions(everyoneRole, view_channel=False)


                # Chat channel
                chatChannel = discordhelper.get_channel_from_category_by_name(dayCat, chatChannelName)
                if not chatChannel:
                    chatChannel = await dayCat.create_text_channel(chatChannelName)
                await chatChannel.set_permissions(botRole, view_channel=True)
                if not guildPlayerRole:
                    await chatChannel.set_permissions(everyoneRole, view_channel=False)


                # Town Square 
                townSquareChannel = discordhelper.get_channel_from_category_by_name(dayCat, townSquareChannelName)
                if not townSquareChannel:
                    townSquareChannel = await dayCat.create_voice_channel(townSquareChannelName)

                if guildPlayerRole:
                    await dayCat.set_permissions(everyoneRole, view_channel=False)
                    await townSquareChannel.set_permissions(guildPlayerRole, view_channel=True)
            
            
                # Extra day channels
                for extraChannelName in extraChannelNames:
                    extraChannel = discordhelper.get_channel_from_category_by_name(dayCat, extraChannelName)
                    if not extraChannel:
                        extraChannel = await dayCat.create_voice_channel(extraChannelName)
                    if not guildPlayerRole:
                        await extraChannel.set_permissions(everyoneRole, view_channel=False)


                # Night channels
                if allowNightCategory:
                    for c in nightCat.channels:
                        if c.type == discord.ChannelType.voice and c.name == nightChannelName:
                            neededNightChannels = neededNightChannels - 1

                    if neededNightChannels > 0:
                        for x in range(neededNightChannels):
                            await nightCat.create_voice_channel(nightChannelName)


                # Calling addTown
                (post, info) = await self.resolveTownInfo(ctx, moverChannelName, townSquareChannelName, dayCatName, nightCatName, gameStRoleName, gameVillagerRoleName, chatChannelName)

                if not post:
                    await ctx.send("There was a problem creating the town of **" + townName + "**.")
                    return
                
                await ctx.send("The town of **" + townName + "** has been created!")
                await self.addTownInternal(ctx, post, info, message_if_exists=False)

            except Exception as ex:
                await discordhelper.send_error_to_author(ctx)

    
    @commands.command(name='destroyTown', aliases=['destroytown'], help='Destroy everything created from the \'createTown\' command')
    async def destroyTown(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            params = shlex.split(ctx.message.content)

            guild = ctx.guild
        
            usageStr = "Usage: `<town name>`"

            if len(params) < 2:
                await ctx.send(f"Too few params to `{COMMAND_PREFIX}destroyTown`. " + usageStr)
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
                nightCat = discordhelper.get_category_by_name(guild, nightCatName)
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
                dayCat = discordhelper.get_category_by_name(guild, dayCatName)
                if dayCat:
                    chatChannel = discordhelper.get_channel_from_category_by_name(dayCat, chatChannelName)
                    if chatChannel and chatChannel.type == discord.ChannelType.text:
                        #print("I want to delete: " + chatChannel.name)
                        await chatChannel.delete()
                    
                    townSquareChannel = discordhelper.get_channel_from_category_by_name(dayCat, townSquareChannelName)
                    if townSquareChannel and townSquareChannel.type == discord.ChannelType.voice:
                        #print("I want to delete: " + townSquareChannel.name)
                        await townSquareChannel.delete()

                    for extraChannelName in extraChannelNames:
                        extraChannel = discordhelper.get_channel_from_category_by_name(dayCat, extraChannelName)
                        if extraChannel and extraChannel.type == discord.ChannelType.voice:
                            #print("I want to delete: " + extraChannel.name)
                            await extraChannel.delete()

                    moverChannel = discordhelper.get_channel_from_category_by_name(dayCat, moverChannelName)
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
                gameVillagerRole = discordhelper.get_role_by_name(guild, gameVillagerRoleName)
                if gameVillagerRole:
                    if success:
                        #print("I want to delete: " + gameVillagerRole.name)
                        await gameVillagerRole.delete()
                    else:
                        notDeleted.append("**" + gameVillagerRole.name + "** (role)")
                
                gameStRole = discordhelper.get_role_by_name(guild, gameStRoleName)
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
                await discordhelper.send_error_to_author(ctx)


    async def sendEmbed(self, ctx, townInfo):
        guild = ctx.guild
        embed = discord.Embed(title=f'{guild.name} // {townInfo.dayCategory.name}', description=f'Created {townInfo.timestamp} by {townInfo.authorName}', color=0xcc0000)
        embed.add_field(name="Control Channel", value=townInfo.controlChannel.name, inline=False)
        embed.add_field(name="Town Square", value=townInfo.townSquare.name, inline=False)
        embed.add_field(name="Chat Channel", value=townInfo.chatChannel and townInfo.chatChannel.name or "<None>", inline=False)
        embed.add_field(name="Day Category", value=townInfo.dayCategory.name, inline=False)
        embed.add_field(name="Night Category", value=townInfo.nightCategory and townInfo.nightCategory.name or "<None>", inline=False)
        embed.add_field(name="Storyteller Role", value=townInfo.storyTellerRole.name, inline=False)
        embed.add_field(name="Villager Role", value=townInfo.villagerRole.name, inline=False)
        await ctx.send(embed=embed)


class GameplayCog(commands.Cog, name='Gameplay'):
    
    game:gameplay.GameplayImpl

    def __init__(self, bot):
        self.bot = bot
        self.votetimer = votetimer.VoteTimer(bot, self.move_users_for_vote)

        # Start the timer if we have active games
        if g_dbActiveGames.count_documents({}) > 0:
            self.cleanupInactiveGames.start() 

        # TODO this is a hack
        rec = gameplay.IActivityRecorder()
        clean = gameplay.ICleanup()
        self.game = gameplay.GameplayImpl(rec, clean, COMMAND_PREFIX)

    def cog_unload(self):
        self.cleanupInactiveGames.cancel()

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

        msg = ""

        # find all guild members with the Current Game role
        prevPlayers = set()
        prevSts = set()
        for m in guild.members:
            if info.villagerRole in m.roles:
                prevPlayers.add(m)
            if info.storyTellerRole in m.roles:
                prevSts.add(m)

        nameList = ", ".join(discordhelper.user_names(prevPlayers))
        msg += f"Removed **{info.villagerRole.name}** role from: **{nameList}**"
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

        nameList = ", ".join(discordhelper.user_names(prevSts))
        msg += f"\nRemoved **{info.storyTellerRole.name}** role from: **{nameList}**"

        for prevSt in prevSts:
            # remove storyteller role and name from storyteller
            await prevSt.remove_roles(info.storyTellerRole)
            if prevSt.display_name.startswith('(ST) '):
                newnick = prevSt.display_name[5:]
                try:
                    await prevSt.edit(nick=newnick)
                except:
                    pass

        return msg


    # End the game and remove all the roles, permissions, etc
    @commands.command(name='endGame', aliases=['endgame'], help='End the current game and reset all permissions, roles, names, etc.')
    async def onEndGame(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            guild = ctx.guild

            info = self.bot.getTownInfo(ctx)

            msg = await self.onEndGameInternal(guild, info)
            await ctx.send(msg)

            self.removeActiveGame(guild, ctx.channel)

        except Exception as ex:
            await discordhelper.send_error_to_author(ctx)

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

        msg = await self.game.set_storytellers(info, sts)

        await ctx.send(msg)

    # Set the players in the normal voice channels to have the 'Current Game' role, granting them access to whatever that entails
    @commands.command(name='currGame', aliases=['currgame', 'curgame', 'curGame'], help='Set the current users in all standard BotC voice channels as players in a current game, granting them roles to see channels associated with the game.')
    async def onCurrGame(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            info:botctypes.TownInfo = self.bot.getTownInfo(ctx)

            message:str = await self.game.current_game(info, ctx.author)

            await ctx.send(message)

        except Exception as ex:
            await discordhelper.send_error_to_author(ctx)


    # Given a list of users and a name string, find the user with the closest name
    def getClosestUser(self, userlist, name):
        for u in userlist:
            # See if anybody's name starts with what was sent
            uname = discordhelper.get_user_name(u).lower()

            if uname.startswith(name.lower()):
                return u
        
        return None

    # Common code for parsing evil and lunatic commands
    async def processMinionMessage(self, ctx, users):

        # Split the message allowing quoted substrings
        params = shlex.split(ctx.message.content)
        # Delete the input message
        await ctx.message.delete()

        # Grab the demon and list of minions
        demon = params[1]
        minions = params[2:]

        if len(minions) == 0:
            await discordhelper.send_error_to_author(ctx, f"It seems you forgot to specify any minions!")
            return (False, None, None)

        # Get the users from the names
        demonUser = self.getClosestUser(users, demon)
        minionUsers = list(map(lambda x: self.getClosestUser(users, x), minions))

        info = self.bot.getTownInfo(ctx)
        categories = [info.dayCategory.name]
        if info.nightCategory:
            categories.append(info.nightCategory.name)
        catString = ', '.join(categories)

        # Error messages for users not found
        if demonUser is None:
            await discordhelper.send_error_to_author(ctx, f"Couldn't find user **{demon}** in these categories: {catString}.")
            return (False, None, None)

        for (i, m) in enumerate(minionUsers):
            if m is None:
                await discordhelper.send_error_to_author(ctx, f"Couldn't find user **{minions[i]}** in these categories: {catString}.")
                return (False, None, None)

        return (True, demonUser, minionUsers)

    # Send a message to the demon
    async def sendDemonMessage(self, demonUser, minionUsers):
        demonMsg = f"{discordhelper.get_user_name(demonUser)}: You are the **demon**. Your minions are: "
        minionNames = discordhelper.user_names(minionUsers)
        demonMsg += ', '.join(minionNames)
        await demonUser.send(demonMsg)

    # Command to send fake evil info to the Lunatic
    # Works the same as evil, but doesn't message the minions
    @commands.command(name='lunatic', help=f'Send fake evil info to the Lunatic. Format is `{COMMAND_PREFIX}lunatic <Lunatic> <fake minion> <fake minion> <fake minion>`')
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
            await discordhelper.send_error_to_author(ctx)

    # Command to send demon/minion info to the Demon and Minions
    @commands.command(name='evil', help=f'Send evil info to evil team. Format is `{COMMAND_PREFIX}evil <demon> <minion> <minion> <minion>`')
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
                otherMinions = discordhelper.user_names(minionUsers)
                otherMinions.remove(discordhelper.get_user_name(m))

                otherMinionsMsg = ', '.join(otherMinions)
                formattedMsg = minionMsg.format(discordhelper.get_user_name(m), discordhelper.get_user_name(demonUser), otherMinionsMsg)
                await m.send(formattedMsg)

            await ctx.send("The Evil team has been informed...")
                
        except Exception as ex:
            await discordhelper.send_error_to_author(ctx)

    # Move users to the night cottages
    @commands.command(name='night', help='Move users to Cottages in the BotC - Nighttime category')
    async def onNight(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            # do role switching for active game
            await self.onCurrGame(ctx)

            # get channels we care about
            info = self.bot.getTownInfo(ctx)
            
            if not info.nightCategory:
                await ctx.send(f'This town does not have a Night category and therefore does not support the `{COMMAND_PREFIX}day` or `{COMMAND_PREFIX}night` commands. If you want to change this, please add a Night category and use the `!addTown` command to update the town info!')
                return

            # get list of users in town square   
            users = list(info.villagers)
            users.sort(key=lambda x: x.display_name.lower())
            cottages = list(info.nightChannels)
            cottages.sort(key=lambda x: x.position)

            await ctx.send(f'Moving {len(info.storyTellers)} storytellers and {len(users)} villagers to Cottages!')
            
            # Put all storytellers in the first cottage
            firstCottage = cottages[0]
            for st in info.storyTellers:
                await st.move_to(firstCottage)

            # And everybody else in the rest
            cottages = cottages[1:]

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
            await discordhelper.send_error_to_author(ctx)

    # Move users from night Cottages back to Town Square
    @commands.command(name='day', help='Move players from Cottages back to Town Square')
    async def onDay(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            info = self.bot.getTownInfo(ctx)

            if not info.nightCategory:
                await ctx.send(f'This town does not have a Night category and therefore does not support the `{COMMAND_PREFIX}day` or `{COMMAND_PREFIX}night` commands. If you want to change this, please add a Night category and use the `!addTown` command to update the town info!')
                return

            # get users in night channels
            users = list()
            for c in info.nightChannels:
                users.extend(c.members)
                # Take away permission overwrites for their cottage
                for m in c.members:
                    await c.set_permissions(m, overwrite=None)

            await ctx.send(f'Moving {len(users)} players from Cottages to **{info.townSquare.name}**.')

            # randomize the order we bring people back
            random.shuffle(users)

            # move them to Town Square
            for user in users:
                await user.move_to(info.townSquare)

        except Exception as ex:
            await discordhelper.send_error_to_author(ctx)

    async def move_users_for_vote(self, town_info):
        # get users in day channels other than Town Square
        users = list()
        for c in town_info.dayChannels:
            if c != town_info.townSquare:
                users.extend(c.members)

        await town_info.controlChannel.send(f'Moving {len(users)} players from daytime channels to **{town_info.townSquare.name}**.')

        # move them to Town Square
        for user in users:
            await user.move_to(town_info.townSquare)


    # Move users from other daytime channels back to Town Square
    @commands.command(name='vote', help='Move players from daytime channels back to Town Square')
    async def onVote(self, ctx):
        if not await self.isValid(ctx):
            return

        try:
            info = self.bot.getTownInfo(ctx)
            await self.move_users_for_vote(info)
        
        except Exception as ex:
            await discordhelper.send_error_to_author(ctx)

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
            # Fetch the full info for this guild/channel combo
            lookupQuery = { "guild" : guildId, "controlChannelId" : rec["channel"] }
            doc = g_dbGuildInfo.find_one(lookupQuery)
            if doc:
                guild = self.bot.get_guild(guildId)
                if guild:
                    (townValid, townError) = isTownValid(guild, doc)
                    if townValid:
                        info = TownInfo(guild, doc)
                        try:
                            await self.onEndGameInternal(guild, info)
                            numEnded = numEnded + 1
                            print(f"Ended game in guild {guildId}")
                            g_dbActiveGames.delete_one(lookupQuery)
                        except Exception:
                            pass
                    else:
                        print(f"Couldn't end game in guild {guildId} due to {townError}")
                        g_dbActiveGames.delete_one(lookupQuery)
                else:
                    print(f"Couldn't find guild with id {guildId}, may have been deleted")
                    g_dbActiveGames.delete_one(lookupQuery)

        if numEnded > 0:
            print(f'Ended {numEnded} inactive game(s)')

        # Remove all the inactive towns from our list just in case any fell through
        result = g_dbActiveGames.delete_many(inactiveQuery)

        # If nothing is left active at all, stop the timer
        if g_dbActiveGames.count_documents({}) == 0:
            print("No remaining active games, stopping checks")
            self.cleanupInactiveGames.stop()

    
    @cleanupInactiveGames.before_loop
    async def beforecleanupInactiveGames(self):
        await self.bot.wait_until_ready()

    # Start the vote timer
    @commands.command(name='voteTimer', aliases=['vt', 'votetimer'], help=f'Start a countdown to voting time.\n\nUsage: {COMMAND_PREFIX}votetimer <time string>\n\nTime string can look like: "5 minutes 30 seconds" or "5:30" or "5m30s"')
    async def start_timer(self, ctx):
        await self.perform_action_reporting_errors(self.votetimer.start_timer, ctx)

    # Stop an ongoing vote timer
    @commands.command(name='stopVoteTimer', aliases=['svt', 'stopvotetimer'], help=f'Stop an existing countdown to voting.\n\nUsage: {COMMAND_PREFIX}stopvotetimer')
    async def stop_timer(self, ctx):
        await self.perform_action_reporting_errors(self.votetimer.stop_timer, ctx)

    async def perform_action_reporting_errors(self, action, ctx):
        try:
            message = await action(ctx)
            if message != None:
                await ctx.send(message)

        except Exception as ex:
            await discordhelper.send_error_to_author(ctx)

class LookupCog(commands.Cog, name='Lookup'):
    def __init__(self, bot, db):
        self.bot = bot
        self.lookup = lookup.Lookup(db)

    # Perform a role lookup
    @commands.command(name='character', aliases=['role', 'char'], help=f'Look up a character by name.\n\nUsage: {COMMAND_PREFIX}character <character name>')
    async def role_lookup(self, ctx):
        await self.perform_action_reporting_errors(self.lookup.role_lookup, ctx)

    # Add a script url
    @commands.command(name='addScript', aliases=['addscript'], help=f'Add a script by its json.\n\nUsage: {COMMAND_PREFIX}addScript <url to json>')
    async def add_script(self, ctx):
        await self.perform_control_channel_action_reporting_errors(self.lookup.add_script, ctx)

    # Remove a script url
    @commands.command(name='removeScript', aliases=['removescript'], help=f'Remove a script by its json.\n\nUsage: {COMMAND_PREFIX}removeScript <url to json>')
    async def remove_script(self, ctx):
        await self.perform_control_channel_action_reporting_errors(self.lookup.remove_script, ctx)

    # Refresh the list of scripts
    @commands.command(name='refreshScripts', aliases=['refreshscripts'], help=f'Refresh all scripts added via {COMMAND_PREFIX}addScript.\n\nUsage: {COMMAND_PREFIX}refreshScripts')
    async def refresh_scripts(self, ctx):
        await self.perform_action_reporting_errors(self.lookup.refresh_scripts, ctx)

    # List all known scripts
    @commands.command(name='listScripts', aliases=['listscripts'], help=f'List all the scripts added via {COMMAND_PREFIX}addScript.\n\nUsage: {COMMAND_PREFIX}listScripts')
    async def list_scripts(self, ctx):
        await self.perform_control_channel_action_reporting_errors(self.lookup.list_scripts, ctx)

    async def perform_control_channel_action_reporting_errors(self, action, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            town_info = self.bot.getTownInfo(ctx)
            if town_info:
                await  self.perform_action_reporting_errors_internal(action, ctx)
            else:
                await ctx.send('This action can only be performed from a town control channel.')

    async def perform_action_reporting_errors(self, action, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            await self.perform_action_reporting_errors_internal(action, ctx)

    async def perform_action_reporting_errors_internal(self, action, ctx):
        try:
            message = await action(ctx)
            if message != None:
                await ctx.send(message)

        except Exception as ex:
            await discordhelper.send_error_to_author(ctx)



bot = botcBot(command_prefix=COMMAND_PREFIX, intents=intents, description='Bot to manage playing Blood on the Clocktower via Discord')
bot.add_cog(SetupCog(bot))
bot.add_cog(GameplayCog(bot))
bot.add_cog(AnnouncerCog(bot))
bot.add_cog(LookupCog(bot, db))
bot.run(TOKEN)