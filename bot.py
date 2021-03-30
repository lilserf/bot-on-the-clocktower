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
intents = discord.Intents().default()
intents.members = True

help_command = commands.DefaultHelpCommand(
    no_category = 'Commands'
)

bot = commands.Bot(command_prefix='!', intents=intents, description='Bot to move users between BotC channels', help_command=help_command)

###########
# Required permissions:
#
# Manage Roles, View Channels, Change Nicknames, Manage Nicknames
# Send Messages, Manage Messages
# Move Members

################
# Connect to mongo and get our DB object used globally throughout this file
MONGO_CONNECT = os.getenv('MONGO_CONNECT')
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

# Parse all the params for addTown, sanity check them, and return useful dicts
async def resolveTownInfo(ctx, params):
    
    guild = ctx.guild

    if len(params) < 7:
        await ctx.send("Too few params to `!addTown`: should provide `<control channel> <townsquare channel> <day category> <night category> <ST role> <player role>`")

    dayCatName = params[3]
    nightCatName = params[4]

    dayCat = getCategoryByName(guild, dayCatName)
    nightCat = getCategoryByName(guild, nightCatName)

    controlName = params[1]
    townSquareName = params[2]

    controlChan = getChannelFromCategoryByName(dayCat, controlName)
    townSquare = getChannelFromCategoryByName(dayCat, townSquareName)

    stRoleName = params[5]
    villagerName = params[6]

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

async def sendEmbed(ctx, townInfo):
    guild = ctx.guild
    embed = discord.Embed(title=f'{guild.name} // {townInfo.dayCategory.name}', description=f'Created {townInfo.timestamp} by {townInfo.authorName}', color=0xcc0000)
    embed.add_field(name="Control Channel", value=townInfo.controlChannel.name, inline=False)
    embed.add_field(name="Town Square", value=townInfo.townSquare.name, inline=False)
    embed.add_field(name="Day Category", value=townInfo.dayCategory.name, inline=False)
    embed.add_field(name="Night Category", value=townInfo.nightCategory.name, inline=False)
    embed.add_field(name="Storyteller Role", value=townInfo.storyTellerRole.name, inline=False)
    embed.add_field(name="Villager Role", value=townInfo.villagerRole.name, inline=False)
    await ctx.send(embed=embed)


@bot.command(name='townInfo', aliases=['towninfo'], help='Show the stored info about the channels and roles that make up this town')
async def townInfo(ctx):

    info = getTownInfo(ctx)

    await sendEmbed(ctx, info)


@bot.command(name='addTown', help='Add a game on this server')
async def addTown(ctx):
    guild = ctx.guild

    params = shlex.split(ctx.message.content)

    (post, info) = await resolveTownInfo(ctx, params)

    if not post:
        return

    # Check if a town already exists
    query = { 
        "guild" : post["guild"],
        "dayCategoryId" : post["dayCategoryId"],
    }

    existing = guildInfo.find_one(query)

    if existing:
        await ctx.send(f'Found an existing town on this server using daytime category `{post["dayCategory"]}`, modifying it!')

    # Upsert the town into place
    print(f'Adding a town to guild {post["guild"]} with control channel [{post["controlChannel"]}], day category [{post["dayCategory"]}], night category [{post["nightCategory"]}]')
    guildInfo.replace_one(query, post, True)

    await sendEmbed(ctx, info)


@bot.command(name='removeTown', help='Remove a game on this server')
async def removeTown(ctx):
    guild = ctx.guild

    params = shlex.split(ctx.message.content)

    post = {"guild" : guild.id,
        "dayCategory" : params[1]}

    print(f'Removing a game from guild {post.guild} with day category [{post.dayCategory}]')
    guildInfo.delete_one(post)


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


# Get a well-defined TownInfo based on the stored DB info for this guild
def getTownInfo(ctx):

    query = { "guild" : ctx.guild.id, "controlChannelId" : ctx.channel.id }
    doc = guildInfo.find_one(query)

    if doc:
        return TownInfo(ctx, doc)
    else:
        return None

@bot.event
async def on_ready():
    print(f'{bot.user.name} has connected to Discord!')

# Given a list of users, return a list of their names
def userNames(users):
    return list(map(lambda x: x.display_name, users))

# Helper to see if a command context is in a valid channel etc - used by all the commands
async def isValid(ctx):

    query = { "guild" : ctx.guild.id }
    result = guildInfo.find(query)
    chanIds = map(lambda x: x["controlChannelId"], result)

    if isinstance(ctx.channel, discord.TextChannel):
        if ctx.channel.id in chanIds:
            return True
    elif isinstance(ctx.channel, discord.DMChannel):
        await ctx.send(f"Whoops, you probably meant to send that in a text channel instead of a DM! Sorry, mate.")
    return False

# End the game and remove all the roles, permissions, etc
@bot.command(name='endGame', aliases=['endgame'], help='End the current game and reset all permissions, roles, names, etc.')
async def onEndGame(ctx):
    if not await isValid(ctx):
        return

    try:
        guild = ctx.guild

        info = getTownInfo(ctx)

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
        await sendErrorToAuthor(ctx)


# Set the players in the normal voice channels to have the 'Current Game' role, granting them access to whatever that entails
@bot.command(name='currGame', aliases=['currgame', 'curgame', 'curGame'], help='Set the current users in all standard BotC voice channels as players in a current game, granting them roles to see channels associated with the game.')
async def onCurrGame(ctx):
    if not await isValid(ctx):
        return

    try:
        guild = ctx.guild

        info = getTownInfo(ctx)

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
            removeMsg += ', '.join(userNames(remove))
            for m in remove:
                await m.remove_roles(info.villagerRole)
            await ctx.send(removeMsg)

        # add any new players
        if len(add) > 0:
            addMsg = f"Added {info.villagerRole.name} role to: "
            addMsg += ', '.join(userNames(add))
            for m in add:
                await m.add_roles(info.villagerRole)
            await ctx.send(addMsg)

    except Exception as ex:
        await sendErrorToAuthor(ctx)

# Given a list of users and a name string, find the user with the closest name
def getClosestUser(userlist, name):
    for u in userlist:
        # See if anybody's name starts with what was sent
        if u.display_name.lower().startswith(name.lower()):
            return u
    
    return None

# Helper to send a message to the author of the command about what they did wrong
async def sendErrorToAuthor(ctx, error=None):
    if error is not None:
        formatted = error
    else:
        formatted = '```\n' + traceback.format_exc(3) + '\n```'
        traceback.print_exc()
    await ctx.author.send(f"Alas, an error has occurred:\n{formatted}\n(from message `{ctx.message.content}`)")

# Common code for parsing !evil and !lunatic commands
async def processMessage(ctx, users):

    # Split the message allowing quoted substrings
    params = shlex.split(ctx.message.content)
    # Delete the input message
    await ctx.message.delete()

    # Grab the demon and list of minions
    demon = params[1]
    minions = params[2:]

    if len(minions) == 0:
        await sendErrorToAuthor(ctx, f"It seems you forgot to specify any minions!")
        return (False, None, None)

    # Get the users from the names
    demonUser = getClosestUser(users, demon)
    minionUsers = list(map(lambda x: getClosestUser(users, x), minions))

    info = getTownInfo(ctx)
    categories = [info.dayCategory.name, info.nightCategory.name]
    catString = ', '.join(categories)

    # Error messages for users not found
    if demonUser is None:
        await sendErrorToAuthor(ctx, f"Couldn't find user **{demon}** in these categories: {catString}.")
        return (False, None, None)

    for (i, m) in enumerate(minionUsers):
        if m is None:
            await sendErrorToAuthor(ctx, f"Couldn't find user **{minions[i]}** in these categories: {catString}.")
            return (False, None, None)

    return (True, demonUser, minionUsers)

# Send a message to the demon
async def sendDemonMessage(demonUser, minionUsers):
    demonMsg = f"{demonUser.display_name}: You are the **demon**. Your minions are: "
    minionNames = userNames(minionUsers)
    demonMsg += ', '.join(minionNames)
    await demonUser.send(demonMsg)

# Command to send fake evil info to the Lunatic
# Works the same as !evil, but doesn't message the minions
@bot.command(name='lunatic', help='Send fake evil info to the Lunatic. Format is `!lunatic <Lunatic> <fake minion> <fake minion> <fake minion>`')
async def onLunatic(ctx):
    if not await isValid(ctx):
        return

    try:
        info = getTownInfo(ctx)
        users = info.activePlayers
        (success, demonUser, minionUsers) = await processMessage(ctx, users)

        if not success:
            return

        await sendDemonMessage(demonUser, minionUsers)

    except Exception as ex:
        await sendErrorToAuthor(ctx)

# Command to send demon/minion info to the Demon and Minions
@bot.command(name='evil', help='Send evil info to evil team. Format is `!evil <demon> <minion> <minion> <minion>`')
async def onEvil(ctx):
    if not await isValid(ctx):
        return

    try:
        info = getTownInfo(ctx)
        users = info.activePlayers
        (success, demonUser, minionUsers) = await processMessage(ctx, users)

        if not success:
            return

        await sendDemonMessage(demonUser, minionUsers)

        minionMsg = "{}: You are a **minion**. Your demon is: {}."
        
        if len(minionUsers) > 1:
            minionMsg += " Your fellow minions are: {}."

        for m in minionUsers:
            otherMinions = userNames(minionUsers)
            otherMinions.remove(m.display_name)

            otherMinionsMsg = ', '.join(otherMinions)
            formattedMsg = minionMsg.format(m.display_name, demonUser.display_name, otherMinionsMsg)
            await m.send(formattedMsg)

        await ctx.send("The Evil team has been informed...")
            
    except Exception as ex:
        await sendErrorToAuthor(ctx)

# Move users to the night cottages
@bot.command(name='night', help='Move users to Cottages in the BotC - Nighttime category')
async def onNight(ctx):
    if not await isValid(ctx):
        return

    try:
        # do role switching for active game first!
        await onCurrGame(ctx)

        await ctx.send('Moving users to Cottages!')
        
        # get channels we care about
        info = getTownInfo(ctx)

        # get list of users in town square   
        users = list(info.activePlayers)
        users.sort(key=lambda x: x.display_name)
        cottages = list(info.nightChannels)
        cottages.sort(key=lambda x: x.position)

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
        await sendErrorToAuthor(ctx)

# Move users from night Cottages back to Town Square
@bot.command(name='day', help='Move users from Cottages back to Town Square')
async def onDay(ctx):
    if not await isValid(ctx):
        return

    try:
        info = getTownInfo(ctx)
        await ctx.send(f'Moving users from Cottages to {info.townSquare.name}.')

        # get users in night channels
        users = list()
        for c in info.nightChannels:
            users.extend(c.members)
            # Take away permission overwrites for their cottage
            for m in c.members:
                await c.set_permissions(m, overwrite=None)

        # randomize the order we bring people back
        random.shuffle(users)

        # move them to Town Square
        for user in users:
            await user.move_to(info.townSquare)

    except Exception as ex:
        await sendErrorToAuthor(ctx)

# Move users from other daytime channels back to Town Square
@bot.command(name='vote', help='Move users from other channels back to Town Square')
async def onVote(ctx):
    if not await isValid(ctx):
        return

    try:
        await ctx.send(f'Moving users from other areas to {info.townSquare.name}.')

        info = getTownInfo(ctx)
        
        # get users in day channels other than Town Square
        users = list()
        for c in info.dayChannels:
            if c != info.townSquare:
                users.extend(c.members)

        # move them to Town Square
        for user in users:
            await user.move_to(info.townSquare)
    
    except Exception as ex:
        await sendErrorToAuthor(ctx)


bot.run(TOKEN)
