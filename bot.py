import os

import discord
import json
from dotenv import load_dotenv
from discord.ext import commands
from operator import itemgetter, attrgetter
import shlex

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

    d['storytellerRole'] = discord.utils.find(lambda r: r.name=='BotC Current Storyteller', guild.roles)

    return d

@bot.event
async def on_ready():
    print(f'{bot.user.name} has connected to Discord!')

# Given a list of users and a name string, find the user with the closest name
def getClosestUser(userlist, name):
    for u in userlist:
        # Try an exact match
        if u.display_name.lower() == name.lower():
            return u
        else:
            # Okay, try an exact match of the first word
            splits = u.display_name.split(' ')

            if splits[0].lower() == name.lower():
                return u
            else:
                # Okay, try the first word *starting* with the string
                if splits[0].lower().startswith(name.lower()):
                    return u
    
    return None


# Common code for parsing !evil and !lunatic commands
async def processMessage(ctx, users):

    # Split the message allowing quoted substrings
    params = shlex.split(ctx.message.content)
    # Delete the input message
    await ctx.message.delete()

    # Grab the demon and list of minions
    demon = params[1]
    minions = params[2:]

    # Get the users from the names
    demonUser = getClosestUser(users, demon)
    minionUsers = list(map(lambda x: getClosestUser(users, x), minions))

    # Error messages for users not found
    if demonUser is None:
        await ctx.send(f"Unknown user '{demon}'.")
        return (False, None, None)

    for (i, m) in enumerate(minionUsers):
        if m is None:
            await ctx.send(f"Unknown user '{minions[i]}'")
            return (False, None, None)

    return (True, demonUser, minionUsers)

# Send a message to the demon
async def sendDemonMessage(demonUser, minionUsers):
    demonMsg = f"{demonUser.display_name}: You are the **demon**. Your minions are: "
    for m in minionUsers:
        demonMsg += m.display_name + " "
    await demonUser.send(demonMsg)

# Command to send fake evil info to the Lunatic
# Works the same as !evil, but doesn't message the minions
@bot.command(name='lunatic', help='Send fake evil info to the Lunatic. Format is `!lunatic <Lunatic> <fake minion> <fake minion> <fake minion>`')
async def onLunatic(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

    try:
        info = getInfo(ctx)
        users = info['townSquare'].members
        (success, demonUser, minionUsers) = await processMessage(ctx, users)

        if not success:
            return

        await sendDemonMessage(demonUser, minionUsers)

    except Exception as ex:
        await ctx.send('`' + repr(ex) + '`')

# Command to send demon/minion info to the Demon and Minions
@bot.command(name='evil', help='Send evil info to evil team. Format is `!evil <demon> <minion> <minion> <minion>`')
async def onEvil(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

    try:
        info = getInfo(ctx)
        users = info['townSquare'].members
        (success, demonUser, minionUsers) = await processMessage(ctx, users)

        if not success:
            return

        await sendDemonMessage(demonUser, minionUsers)

        minionMsg = "{}: You are a **minion**. Your demon is {}"

        for m in minionUsers:
            formattedMsg = minionMsg.format(m.display_name, demonUser.display_name)
            await m.send(formattedMsg)

        await ctx.send("The Evil team has been informed...")
        
    except Exception as ex:
        await ctx.send('`' + repr(ex) + '`')

# Move users to the night cottages
@bot.command(name='night', help='Move users to Cottages in the BotC - Nighttime category')
async def onNight(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

    try:

        await ctx.send('Moving users to Cottages!')
        
        # get channels we care about
        info = getInfo(ctx)

        # grant the storyteller the Current Storyteller role
        storyteller = ctx.message.author
        role = info['storytellerRole']
        await storyteller.add_roles(role)

        # get list of users in town square   
        users = info['townSquare'].members
        cottages = list(info['nightChannels'])
        cottages.sort(key=lambda x: x.position)

        # pair up users with cottages
        pairs = list(map(lambda x, y: (x,y), users, cottages))

        # move each user to a cottage
        for (user, cottage) in sorted(pairs, key=lambda x: x[0].name):
            print("Moving %s to %s %d" % (user.name, cottage.name, cottage.id))
            # remove the Current Storyteller role from other folks we're moving
            if user.id != storyteller.id:
                await user.remove_roles(role)
            await user.move_to(cottage)

    except Exception as ex:
        await ctx.send('`' + repr(ex) + '`')

# Move users from night Cottages back to Town Square
@bot.command(name='day', help='Move users from Cottages back to Town Square')
async def onDay(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

    try:
        await ctx.send('Moving users from Cottages to Town Square.')

        info = getInfo(ctx)

        # get users in night channels
        users = list()
        for c in info['nightChannels']:
            users.extend(c.members)

        # move them to Town Square
        for user in users:
            await user.move_to(info['townSquare'])

    except Exception as ex:
        await ctx.send('`' + repr(ex) + '`')

# Move users from other daytime channels back to Town Square
@bot.command(name='vote', help='Move users from other channels back to Town Square')
async def onVote(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
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
        await ctx.send('`' + repr(ex) + '`')


bot.run(TOKEN)
