import os

import discord
from dotenv import load_dotenv
from discord.ext import commands

load_dotenv()
TOKEN = os.getenv('DISCORD_TOKEN')

bot = commands.Bot(command_prefix='!')

@bot.event
async def on_ready():
    print(f'{bot.user.name} has connected to Discord!')

@bot.command(name='night', help='Move users to Cottages in the BotC - Nighttime category')
async def on_night(ctx):
    await ctx.send('Moving users to Cottages!')

@bot.command(name='day', help='Move users from Cottages back to Town Square')
async def on_night(ctx):
    await ctx.send('Moving users from Cottages to Town Square.')

@bot.command(name='vote', help='Move users from other channels back to Town Square')
async def on_night(ctx):
    await ctx.send('Moving users from other areas to Town Square.')


bot.run(TOKEN)
