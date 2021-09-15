# pylint: disable=missing-module-docstring, missing-class-docstring, missing-function-docstring, invalid-name
# pylint: disable=assigning-non-slot, broad-except
import os
import shlex
import datetime

from dotenv import load_dotenv
from pymongo import MongoClient
import discord
from discord.ext import commands, tasks

from botctypes import TownInfo
import discordhelper
import votetimer
import announce
import lookup
import gameplay
import messaging
import setup

from callbackscheduler import ICallbackSchedulerFactory, CallbackSchedulerFactory, DiscordExtLoopFactory
from towndb import TownDb, GuildProvider
from pythonwrappers import DateTimeProvider


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
g_cluster = MongoClient(MONGO_CONNECT)
g_db = g_cluster[MONGO_DB]

COMMAND_PREFIX = os.getenv('COMMAND_PREFIX') or '!'

g_dbActiveGames = g_db['ActiveGames']

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

    def get_announce_cog(self):
        return self.get_cog('Version Announcements')

    def get_cleanup_cog(self):
        return self.get_cog('GameCleanup')

    def get_activity_cog(self):
        return self.get_cog('GameActivity')

    def get_gameplay_cog(self):
        return self.get_cog('Gameplay')

    async def on_ready(self):
        print(f'{self.user.name} has connected to Discord! Command prefix: {COMMAND_PREFIX}')
        print('Sending version announcements...')
        announcer = self.get_announce_cog()
        num_sent = await announcer.announce_latest_version()
        print(f'Sent announcements to {num_sent} towns.')

# Announcer cog
class AnnouncerCog(commands.Cog, name='Version Announcements'):
    bot:botcBot

    def __init__(self, bot, mongo, town_db:TownDb):
        self.bot = bot
        self.announcer = announce.Announcer(bot=bot, mongo=mongo, town_db=town_db)

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
            if message is not None:
                await ctx.send(message)

        except Exception:
            await discordhelper.send_error_to_author(ctx)


# Setup cog
class SetupCog(commands.Cog, name='Setup'):
    def __init__(self, *, bot, town_db:TownDb):
        self.bot = bot
        self.town_db = town_db
        self.impl = setup.SetupImpl(town_db=town_db, command_prefix=COMMAND_PREFIX)

    def setToLatestVersion(self, guild):
        self.bot.get_announce_cog().set_to_latest_version(guild)

    @commands.command(name='townInfo', aliases=['towninfo'], help='Show the stored info about the channels and roles that make up this town')
    async def townInfo(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            info = self.town_db.get_town_info(ctx)

            if info is not None:
                await ctx.send(embed=info.make_embed())
            else:
                await ctx.send("Sorry, I couldn't find a town registered to this channel.")

    @commands.command(name='addTown', aliases=['addtown'], help=f'Add a game on this server.\n\nUsage: {COMMAND_PREFIX}addTown <control channel> <town square channel> <day category> <night category> <storyteller role> <villager role> [chat channel]\n\nAlternate usage: {COMMAND_PREFIX}addTown control=<control channel> townSquare=<town square channel> dayCategory=<day category> [nightCategory=<night category>] stRole=<storyteller role> villagerRole=<villager role> [chatChannel=<chat channel>]')
    async def addTown(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            params = shlex.split(ctx.message.content)

            self.setToLatestVersion(ctx.guild)
            # Slice off the !addTown bit
            msg = self.impl.add_town_from_params(guild=ctx.guild, params=params[1:], author=ctx.author)

            await ctx.send(msg)


    @commands.command(name='removeTown', aliases=['removetown'], help='Remove a game on this server')
    async def removeTown(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            guild = ctx.guild

            params = shlex.split(ctx.message.content)
            usageStr = f'Usage: `{COMMAND_PREFIX}removeTown <day category name>` or `{COMMAND_PREFIX}removeTown` alone if run from the town\'s control channel'

            control_channel=None
            day_category_name=None
            if len(params) == 1:
                control_channel = ctx.channel
                day_category_name = control_channel.category.name
            elif len(params) == 2:
                day_category_name = params[1]
            else:
                await ctx.send(f'Unexpected parameters. {usageStr}')
                return

            err = self.impl.remove_town(guild=guild, control_channel=control_channel, day_category_name=day_category_name)

            if err is None:
                embed = discord.Embed(title=f'{guild.name} // {day_category_name}', description='This town is no longer registered.', color=0xcc0000)
                await ctx.send(embed=embed)
            else:
                await ctx.send(err)

    @commands.command(name='setChatChannel', aliases=['setchatchannel', 'setchatchan', 'setchat'], help=f'Set the chat channel associated with this town.\n\nUsage: {COMMAND_PREFIX}setChatChannel <chat channel>')
    async def set_chat_channel(self, ctx):
        if await discordhelper.verify_not_dm_or_send_error(ctx):

            info = self.town_db.get_town_info(ctx)
            if not info:
                await ctx.send('Could not find a town! Are you running this command from the town control channel?')

            params = shlex.split(ctx.message.content)
            if len(params) != 2:
                await ctx.send(f'Incorrect usage for `{COMMAND_PREFIX}setChatChannel`: should provide `<chat channel>`')
                return None

            chat_channel_name = params[1]

            msg = self.impl.set_chat_channel(info, chat_channel_name)

            if msg:
                await ctx.send(msg)
            else:
                await ctx.send(embed=info.make_embed())


    @commands.command(name='createTown', aliases=['createtown'], help=f'Create an entire town on this server, including categories, roles, channels, and permissions.\n\nUsage: {COMMAND_PREFIX}createTown <town name> [server storyteller role] [server player role] [noNight]')
    async def createTown(self, ctx):
        # pylint: disable=too-many-locals, too-many-return-statements, too-many-branches, too-many-statements
        if await discordhelper.verify_not_dm_or_send_error(ctx):

            guild = ctx.guild
            bot_role = discordhelper.get_role_by_name(guild, self.bot.user.name)
            if not bot_role:
                await ctx.send(f"Could not find role for **{self.bot.user.name}**. Cannot proceed! Where did the role go?")
                return

            usageStr = f"Usage: `{COMMAND_PREFIX}createTown <town name> [server storyteller role] [server player role] [noNight]`"

            params = shlex.split(ctx.message.content)
            if len(params) < 2:
                await ctx.send(f"Too few params to `{COMMAND_PREFIX}createTown`. " + usageStr)
                return None

            town_name = params[1]
            if not town_name:
                await ctx.send("No town name provided. " + usageStr)
                return None

            # Check for additional params beyond the required ones
            additional_param_count = 0
            guild_st_role = None
            guild_player_role = None
            allow_night_category = True

            for i in range(2, len(params)):
                p = params[i]
                if p.lower() == "nonight":
                    allow_night_category = False
                else:
                    if additional_param_count == 0:
                        guild_st_role = discordhelper.get_role_by_name(guild, p)
                        if not guild_st_role:
                            await ctx.send("Provided Storyteller Role **" + p + "** not found.")
                            return None
                    elif additional_param_count == 1:
                        guild_player_role = discordhelper.get_role_by_name(guild, p)
                        if not guild_player_role:
                            await ctx.send("Provided Player Role **" + p + "** not found.")
                            return None
                    else:
                        await ctx.send(f"Unknown parameter: {p}")
                        return None

                    additional_param_count = additional_param_count + 1

            await ctx.send(f"Please hold, creating **{town_name}** ...")
            info:TownInfo = None
            msg:str = None

            try:
                (info, msg) = await self.impl.create_town(guild=guild, town_name=town_name, allow_night_category=allow_night_category, guild_st_role=guild_st_role, \
                    guild_player_role=guild_player_role, bot_role=bot_role, author=ctx.author)
            except Exception:
                await discordhelper.send_error_to_author(ctx)
                return

            if msg:
                await ctx.send(msg)
            else:
                await ctx.send("The town of **" + town_name + "** has been created!")
                await ctx.send(embed=info.make_embed())


    @commands.command(name='destroyTown', aliases=['destroytown'], help='Destroy everything created from the \'createTown\' command')
    async def destroyTown(self, ctx):
        # pylint: disable=too-many-locals, too-many-nested-blocks, too-many-branches, too-many-statements
        if await discordhelper.verify_not_dm_or_send_error(ctx):
            params = shlex.split(ctx.message.content)

            guild = ctx.guild
            usageStr = "Usage: `<town name>`"

            if len(params) < 2:
                await ctx.send(f"Too few params to `{COMMAND_PREFIX}destroyTown`. " + usageStr)
                return

            town_name = params[1]
            if not town_name:
                await ctx.send("No town name provided. " + usageStr)
                return

            try:
                await ctx.send(f"Please hold, destroying **{town_name}** ...")
                message = await self.impl.destroy_town(guild=guild, town_name=town_name)
                # Try to send to context, but it may have been a channel we deleted in which case send diretly to the author instead
                try:
                    await ctx.send(message)
                except Exception:
                    await ctx.author.send(message)

            except Exception:
                await discordhelper.send_error_to_author(ctx)



class GameActivityCog(commands.Cog, gameplay.IGameActivity, name='GameActivity'):

    def __init__(self, bot):
        self.bot = bot

    def record_activity(self, guild:discord.Guild, channel:discord.abc.GuildChannel, storytellers:list[discord.Member], active_players:list[discord.Member]):
        # TODO: Needs to get diff of storytellers and active players to properly update the player --> town entries (don't want to have to query the DB a zillion times)
        st_ids = list(map(lambda m: m.id, storytellers))
        player_ids = list(map(lambda m: m.id, active_players))

        post = {
            'guild' : guild.id,
            'channel' : channel.id,
            'storytellerIds' : st_ids,
            'playerIds' : player_ids,
            'lastActivity' : datetime.datetime.now(),
            }
        query = {
            'guild' : guild.id,
            'channel' : channel.id
            }

        # Upsert this record
        g_dbActiveGames.replace_one(query, post, True)

        self.bot.get_cleanup_cog().start()

    def remove_active_game(self, guild:discord.Guild, channel:discord.abc.GuildChannel):
        # Delete this game's record
        g_dbActiveGames.delete_one( {"guild" : guild.id, "channel": channel.id })


class GameCleanupCog(commands.Cog, gameplay.IGameCleanup, name='GameCleanup'):

    def __init__(self, *, bot, town_db:TownDb):
        self.bot = bot
        self.town_db = town_db

    def start(self) -> None:
        # pylint doesn't understand the @tasks.loop decorator
        # pylint: disable=no-member
        if not self.cleanup_inactive_games.is_running():
            self.cleanup_inactive_games.start()

    def stop(self) -> None:
        # pylint doesn't understand the @tasks.loop decorator
        # pylint: disable=no-member
        self.cleanup_inactive_games.stop()

    # Called periodically to try and clean up old games that aren't in progress anymore
    # This is 8 hours, with inactivity at 3, as the expectation is that 3-4 hours is probably
    # at the the common upper limit for a town to be active, so 5 hours should catch most games.
    # For debugging, use the 3-second loop and the 10-second delta below
    #@tasks.loop(seconds=3)
    @tasks.loop(hours=8)
    async def cleanup_inactive_games(self) -> None:
        print("Checking for inactive games")
        # Inactive games are ones that haven't had any activity in the last 3 hours
        inactiveTime = datetime.datetime.now() - datetime.timedelta(hours=3)
        #inactiveTime = datetime.datetime.now() - datetime.timedelta(seconds=10)

        numEnded = 0

        # "Range query" that gives us all documents with a timestamp less than our inactive time
        inactiveQuery = {"lastActivity" : {"$lt": inactiveTime}}

        for rec in g_dbActiveGames.find(inactiveQuery):
            guildId = rec["guild"]
            lookupQuery = { "guild" : guildId, "controlChannelId" : rec["channel"] }
            guild:discord.Guild = self.bot.get_guild(guildId)
            info = self.town_db.find_one_by_control_id(guild, rec["channel"])

            try:
                msg = await self.bot.get_gameplay_cog().end_game(info)
                numEnded = numEnded + 1
                print(f"Ended game in guild {guildId}")
                g_dbActiveGames.delete_one(lookupQuery)
            except Exception:
                print(f"Couldn't end game in guild {guildId}:\n{msg}")
                g_dbActiveGames.delete_one(lookupQuery)

        if numEnded > 0:
            print(f'Ended {numEnded} inactive game(s)')

        # Remove all the inactive towns from our list just in case any fell through
        g_dbActiveGames.delete_many(inactiveQuery)

        # If nothing is left active at all, stop the timer
        if g_dbActiveGames.count_documents({}) == 0:
            # pylint doesn't understand the @tasks.loop decorator
            # pylint: disable=no-member
            print("No remaining active games, stopping checks")
            self.cleanup_inactive_games.stop()

    @cleanup_inactive_games.before_loop
    async def beforecleanupInactiveGames(self):
        await self.bot.wait_until_ready()

class GameplayCog(commands.Cog, name='Gameplay'):

    game:gameplay.GameplayImpl
    role_messager:messaging.RoleMessagerImpl
    town_db:TownDb

    def __init__(self, *, bot, callback_scheduler_factory:ICallbackSchedulerFactory, town_db:TownDb):
        self.bot = bot
        self.town_db = town_db

        self.game = gameplay.GameplayImpl(bot.get_activity_cog(), COMMAND_PREFIX)

        self.role_messager = messaging.RoleMessagerImpl()

        self.votetimer = votetimer.VoteTimer(callback_scheduler_factory, town_db, self.game.phase_vote)

        # Start the timer if we have active games
        if g_dbActiveGames.count_documents({}) > 0:
            self.bot.get_cleanup_cog().start()

    def cog_unload(self):
        self.bot.get_cleanup_cog().stop()

    # Exposed for use by other cogs
    async def end_game(self, info:TownInfo):
        await self.game.end_game(info)

    # End the game and remove all the roles, permissions, etc
    @commands.command(name='endGame', aliases=['endgame'], help='End the current game and reset all permissions, roles, names, etc.')
    async def onEndGame(self, ctx):
        '''Command handler to end the game'''
        await self.perform_action_reporting_errors(lambda inner_ctx: self.game.end_game(self.town_db.get_town_info(inner_ctx)), ctx)

    # Set the current storytellers
    @commands.command(name='setStorytellers', aliases=['setstorytellers', 'setStoryTellers', 'storytellers', 'storyTellers', 'setsts', 'setSts', 'setSTs', 'setST', 'setSt', 'setst', 'sts', 'STs', 'Sts'], help='Set a list of users to be Storytellers.')
    async def on_set_storytellers(self, ctx):
        await self.perform_action_reporting_errors(self.set_storytellers_internal, ctx)

    async def set_storytellers_internal(self, ctx):
        info:TownInfo = self.town_db.get_town_info(ctx)
        names = shlex.split(ctx.message.content)
        sts = list(map(lambda x: discordhelper.get_closest_user(info.active_players, x), names[1:]))
        return await self.game.set_storytellers(info, sts)

    # Set the players in the normal voice channels to have the 'Current Game' role, granting them access to whatever that entails
    @commands.command(name='currGame', aliases=['currgame', 'curgame', 'curGame'], help='Set the current users in all standard BotC voice channels as players in a current game, granting them roles to see channels associated with the game.')
    async def on_curr_game(self, ctx):
        await self.perform_action_reporting_errors(lambda inner_ctx: self.game.current_game(self.town_db.get_town_info(inner_ctx), inner_ctx.author), ctx)

    # Move users to the night cottages
    @commands.command(name='night', help='Move users to Cottages in the BotC - Nighttime category')
    async def on_night(self, ctx):
        await self.perform_action_reporting_errors(lambda inner_ctx: self.game.phase_night(self.town_db.get_town_info(inner_ctx), inner_ctx.author), ctx)

    # Move users from night Cottages back to Town Square
    @commands.command(name='day', help='Move players from Cottages back to Town Square')
    async def on_day(self, ctx):
        await self.perform_action_reporting_errors(lambda inner_ctx: self.game.phase_day(self.town_db.get_town_info(inner_ctx)), ctx)

    # Move users from other daytime channels back to Town Square
    @commands.command(name='vote', help='Move players from daytime channels back to Town Square')
    async def on_vote(self, ctx):
        await self.perform_action_reporting_errors(lambda inner_ctx: self.game.phase_vote(self.town_db.get_town_info(inner_ctx)), ctx)

######################## Messaging Commands

    # Send all Legion players a message
    @commands.command(name='legion', help=f'Send info to all Legion players. Format is `{COMMAND_PREFIX}legion <Legion> <Legion> <Legion> etc`')
    async def on_legion(self, ctx):
        await self.perform_action_reporting_errors(lambda inner_ctx: self.role_messager.inform_legion(self.town_db.get_town_info(inner_ctx), inner_ctx.message, inner_ctx), ctx)

    # Command to send fake evil info to the Lunatic
    # Works the same as evil, but doesn't message the minions
    @commands.command(name='lunatic', help=f'Send fake evil info to the Lunatic. Format is `{COMMAND_PREFIX}lunatic <Lunatic> <fake minion> <fake minion> <fake minion>`')
    async def on_lunatic(self, ctx):
        await self.perform_action_reporting_errors(lambda inner_ctx: self.role_messager.inform_lunatic(self.town_db.get_town_info(inner_ctx), inner_ctx.message, inner_ctx), ctx)

    # Command to send demon/minion info to the Demon and Minions
    @commands.command(name='evil', help=f'Send evil info to evil team. Format is `{COMMAND_PREFIX}evil <demon> <minion> <minion> <minion>`')
    async def on_evil(self, ctx):
        await self.perform_action_reporting_errors(lambda inner_ctx: self.role_messager.inform_evil(self.town_db.get_town_info(inner_ctx), inner_ctx.message, inner_ctx), ctx)

######################## Vote Timer

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
            if message:
                await ctx.send(message)

        except Exception:
            await discordhelper.send_error_to_author(ctx)

class LookupCog(commands.Cog, name='Lookup'):
    def __init__(self, *, bot, db, town_db:TownDb):
        self.bot = bot
        self.town_db = town_db
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
            town_info = self.town_db.get_town_info(ctx)
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
            if message is not None:
                await ctx.send(message)

        except Exception:
            await discordhelper.send_error_to_author(ctx)

            
g_bot = botcBot(command_prefix=COMMAND_PREFIX, intents=intents, description='Bot to manage playing Blood on the Clocktower via Discord')

g_town_db = TownDb(g_db, GuildProvider(g_bot.get_guild))
g_datetime_provider = DateTimeProvider()
g_loop_factory = DiscordExtLoopFactory()
g_callback_scheduler_factory = CallbackSchedulerFactory(g_datetime_provider, g_loop_factory)

g_bot.add_cog(GameActivityCog(g_bot))
g_bot.add_cog(GameCleanupCog(bot=g_bot, town_db=g_town_db))
g_bot.add_cog(SetupCog(bot=g_bot, town_db=g_town_db))
g_bot.add_cog(GameplayCog(bot=g_bot, callback_scheduler_factory=g_callback_scheduler_factory, town_db=g_town_db))
g_bot.add_cog(AnnouncerCog(g_bot, g_db, g_town_db))
g_bot.add_cog(LookupCog(bot=g_bot, db=g_db, town_db=g_town_db))
g_bot.run(TOKEN)
