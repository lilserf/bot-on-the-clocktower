import os

import discord
import json
from dotenv import load_dotenv
from discord.ext import commands
from operator import itemgetter, attrgetter
import shlex
import traceback
import random

load_dotenv()
TOKEN = os.getenv('DISCORD_TOKEN')
intents = discord.Intents().default()
intents.members = True

help_command = commands.DefaultHelpCommand(
    no_category = 'Commands'
)

bot = commands.Bot(command_prefix='!', intents=intents, description='Bot to move users between BotC channels', help_command=help_command)

DAY_CATEGORY = 'BotC - Daytime'
NIGHT_CATEGORY = 'BotC - Nighttime'
TOWN_SQUARE = 'Town Square'
CONTROL_CHANNEL = 'botc_mover'
CURRENT_STORYTELLER = 'BotC Current Storyteller'
CURRENT_GAME = 'BotC Current Game'


###########
# Required permissions:
#
# Manage Roles, View Channels, Change Nicknames, Manage Nicknames
# Send Messages, Manage Messages
# Move Members


# Grab a bunch of common info we need from this particular server,
# based on the assumptions we make about servers
def getInfo(ctx):
    guild = ctx.guild

    d = dict()

    d['dayCategory'] = discord.utils.find(lambda c: c.type == discord.ChannelType.category and c.name == DAY_CATEGORY, guild.channels)
    d['nightCategory'] = discord.utils.find(lambda c: c.type == discord.ChannelType.category and c.name == NIGHT_CATEGORY, guild.channels)
    d['townSquare'] = discord.utils.find(lambda c: c.type == discord.ChannelType.voice and c.name == TOWN_SQUARE, guild.channels)

    d['dayChannels'] = list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id ==  d['dayCategory'].id)
    d['nightChannels'] = list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id == d['nightCategory'].id)

    d['storytellerRole'] = discord.utils.find(lambda r: r.name==CURRENT_STORYTELLER, guild.roles)
    d['currentPlayerRole'] = discord.utils.find(lambda r: r.name==CURRENT_GAME, guild.roles)

    activePlayers = set()
    for c in d['dayChannels']:
        activePlayers.update(c.members)
    for c in d['nightChannels']:
        activePlayers.update(c.members)
    d['activePlayers'] = activePlayers

    return d

@bot.event
async def on_ready():
    print(f'{bot.user.name} has connected to Discord!')

# Given a list of users, return a list of their names
def userNames(users):
    return list(map(lambda x: x.display_name, users))

# Helper to see if a command context is in a valid channel etc - used by all the commands
async def isValid(ctx):
    if isinstance(ctx.channel, discord.TextChannel):
        if ctx.channel.name == CONTROL_CHANNEL:
            return True
    elif isinstance(ctx.channel, discord.DMChannel):
        await ctx.send(f"Whoops, you probably meant to send that in the `{CONTROL_CHANNEL}` channel instead! Sorry, mate.")
    return False

# End the game and remove all the roles, permissions, etc
@bot.command(name='endgame', help='End the current game and reset all permissions, roles, names, etc.')
async def onEndGame(ctx):
    if not await isValid(ctx):
        return

    try:
        guild = ctx.guild

        info = getInfo(ctx)
        playerRole = info['currentPlayerRole']
        stRole = info['storytellerRole']

        # find all guild members with the Current Game role
        prevPlayers = set()
        prevSt = None
        for m in guild.members:
            if playerRole in m.roles:
                prevPlayers.add(m)
            if stRole in m.roles:
                prevSt = m

        # remove game role from players
        for m in prevPlayers:
            await m.remove_roles(playerRole)

        # remove cottage permissions
        for c in info['nightChannels']:
            # Take away permission overwrites for this cottage
            for m in prevPlayers:
                await c.set_permissions(m, overwrite=None)
            await c.set_permissions(prevSt, overwrite=None)

        # remove storyteller role and name from storyteller
        await prevSt.remove_roles(stRole)
        if prevSt.display_name.startswith('(ST) '):
            newnick = m.display_name[5:]
            await m.edit(nick=newnick)

    except Exception as ex:
        await sendErrorToAuthor(ctx)


# Set the players in the normal voice channels to have the 'Current Game' role, granting them access to whatever that entails
@bot.command(name='currgame', help='Set the current users in all standard BotC voice channels as players in a current game, granting them roles to see channels associated with the game.')
async def onCurrGame(ctx):
    if not await isValid(ctx):
        return

    try:
        guild = ctx.guild

        # find all guild members with the Current Game role
        prevPlayers = set()
        for m in guild.members:
            for r in m.roles:
                if r.name == CURRENT_GAME:
                    prevPlayers.add(m)

        info = getInfo(ctx)
        role = info['currentPlayerRole']

        # grant the storyteller the Current Storyteller role
        storyteller = ctx.message.author
        strole = info['storytellerRole']
        await storyteller.add_roles(strole)

        # find all users currently in the channels we play in
        currPlayers = info['activePlayers']

        storyTeller = ctx.message.author

        # take any (ST) off of old storytellers
        for o in currPlayers:
            if o != storyteller and strole in o.roles:
                await o.remove_roles(strole)
            if o != storyTeller and o.display_name.startswith('(ST) '):
                newnick = o.display_name[5:]
                await o.edit(nick=newnick)
        
        # add (ST) to the start of the current storyteller
        if not storyTeller.display_name.startswith('(ST) '):
            await storyTeller.edit(nick=f"(ST) {storyTeller.display_name}")

        # find additions and deletions by diffing the sets
        remove = prevPlayers - currPlayers
        add = currPlayers - prevPlayers

        # remove any stale players
        if len(remove) > 0:
            removeMsg = f"Removed {role.name} role from: "
            removeMsg += ', '.join(userNames(remove))
            for m in remove:
                await m.remove_roles(role)
            await ctx.send(removeMsg)

        # add any new players
        if len(add) > 0:
            addMsg = f"Added {role.name} role to: "
            addMsg += ', '.join(userNames(add))
            for m in add:
                await m.add_roles(role)
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

    info = getInfo(ctx)
    categories = [info['dayCategory'].name, info['nightCategory'].name]
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
        info = getInfo(ctx)
        users = info['activePlayers']
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
        info = getInfo(ctx)
        users = info['activePlayers']
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
        info = getInfo(ctx)

        # get list of users in town square   
        users = list(info['activePlayers'])
        users.sort(key=lambda x: x.display_name)
        cottages = list(info['nightChannels'])
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
        await ctx.send('Moving users from Cottages to Town Square.')

        info = getInfo(ctx)

        # get users in night channels
        users = list()
        for c in info['nightChannels']:
            users.extend(c.members)
            # Take away permission overwrites for their cottage
            for m in c.members:
                await c.set_permissions(m, overwrite=None)

        # randomize the order we bring people back
        random.shuffle(users)

        # move them to Town Square
        for user in users:
            await user.move_to(info['townSquare'])

    except Exception as ex:
        await sendErrorToAuthor(ctx)

# Move users from other daytime channels back to Town Square
@bot.command(name='vote', help='Move users from other channels back to Town Square')
async def onVote(ctx):
    if not await isValid(ctx):
        return

    try:
        await ctx.send('Moving users from other areas to Town Square.')

        info = getInfo(ctx)
        
        # get users in day channels other than Town Square
        users = list()
        for c in info['dayChannels']:
            if c != info['townSquare']:
                users.extend(c.members)

        # move them to Town Square
        for user in users:
            await user.move_to(info['townSquare'])
    
    except Exception as ex:
        await sendErrorToAuthor(ctx)


bot.run(TOKEN)
