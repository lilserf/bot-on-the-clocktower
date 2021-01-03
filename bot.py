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

bot = commands.Bot(command_prefix='!', intents=intents, description='Bot to move users between BotC channels')

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

    return d

@bot.event
async def on_ready():
    print(f'{bot.user.name} has connected to Discord!')

@bot.command(name='night', help='Move users to Cottages in the BotC - Nighttime category')
async def on_night(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

    await ctx.send('Moving users to Cottages!')
    
    # get channels we care about
    info = getInfo(ctx)

    # get list of users in town square   
    users = (info['townSquare'].members)
    cottages = list(info['nightChannels'])

    # pair up users with cottages
    pairs = list(map(lambda x, y: (x,y), users, cottages))

    # move each user to a cottage
    for (user, cottage) in sorted(pairs, key=lambda x: x[0].name):
        print("Moving %s to %s %d" % (user.name, cottage.name, cottage.id))
        await user.move_to(cottage)



@bot.command(name='day', help='Move users from Cottages back to Town Square')
async def on_night(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

    await ctx.send('Moving users from Cottages to Town Square.')

    info = getInfo(ctx)

    # get users in night channels
    users = list()
    for c in info['nightChannels']:
        users.extend(c.members)

    # move them to Town Square
    for user in users:
        await user.move_to(info['townSquare'])


@bot.command(name='vote', help='Move users from other channels back to Town Square')
async def on_night(ctx):
    if ctx.channel.name != CONTROL_CHANNEL:
        return

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


bot.run(TOKEN)
