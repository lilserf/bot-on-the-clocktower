import os

import discord
import json
from dotenv import load_dotenv
from discord.ext import commands
from operator import itemgetter, attrgetter

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

def getClosestUser(userlist, name):
    for u in userlist:

        if u.name.lower() == name.lower():
            return u
        else:
            splits = u.name.split(' ')

            if splits[0].lower() == name.lower():
                return u
            else:
                if splits[0].lower().startswith(name.lower()):
                    return u
    
    return None

@bot.command(name='evil', help='Send evil info to evil team. Format is `!evil <demon> <minion> <minion> <minion>`')
async def onEvil(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

    try:
        info = getInfo(ctx)

        users = info['townSquare'].members

        params = ctx.message.content.split(' ')

        demon = params[1]
        minions = params[2:]

        demonUser = getClosestUser(users, demon)
        minionUsers = list(map(lambda x: getClosestUser(users, x), minions))
        

        demonMsg = f"{demonUser.name}: You are the demon. Your minions are: "
        for m in minionUsers:
            demonMsg += m.name + " "
        await demonUser.send(demonMsg)

        minionMsg = "{}: You are a minion. Your demon is {}"

        for m in minionUsers:
            formattedMsg = minionMsg.format(m.name, demonUser.name)
            await m.send(formattedMsg)

        await ctx.message.delete()
        
    except Exception as ex:
        await ctx.send('`' + repr(ex) + '`')

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
