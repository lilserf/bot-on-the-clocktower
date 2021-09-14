'''Helper functions for dealing with common Discord cases'''
import traceback

import discord
from discord.ext import commands

async def verify_not_dm_or_send_error(ctx:commands.Context) -> bool:
    '''Verify that a message is not a DM; send an error if it is'''
    if isinstance(ctx.channel, discord.DMChannel):
        await ctx.send("Whoops, you probably meant to send that in a text channel instead of a DM!")
        return False
    return True

async def send_error_to_author(ctx:discord.ext.commands.Context, error=None) -> None:
    '''Send an error message to the author of the message in the context'''
    if error:
        formatted = error
    else:
        formatted = '```\n' + traceback.format_exc(3) + '\n```'
        traceback.print_exc()
    await ctx.author.send(f"Alas, an error has occurred:\n{formatted}\n(from message `{ctx.message.content}`)")

def get_channel_from_category_by_name(category:discord.CategoryChannel, name:str) -> discord.abc.GuildChannel:
    '''Get a channel by name given a category'''
    return discord.utils.find(lambda c: (c.type == discord.ChannelType.voice or c.type == discord.ChannelType.text) and c.name == name, category.channels)

def get_category_by_name(guild:discord.Guild, name:str) -> discord.CategoryChannel:
    '''Get a category by name given a Guild'''
    return discord.utils.find(lambda c: c.type == discord.ChannelType.category and c.name == name, guild.channels)

def get_role_by_name(guild:discord.Guild, name:str) -> discord.Role:
    ''' Get a role by name given a Guild'''
    return discord.utils.find(lambda r: r.name==name, guild.roles)

# Get a category by ID or name, preferring ID
def get_category(guild:discord.Guild, name:str, cat_id:int) -> discord.CategoryChannel:
    '''Get a category by ID or name, preferring ID, given a  Guild'''
    cat_by_id = discord.utils.find(lambda c: c.type == discord.ChannelType.category and c.id == cat_id, guild.channels)
    return cat_by_id or discord.utils.find(lambda c: c.type == discord.ChannelType.category and c.name == name, guild.channels)

# Get a channel by ID or name, preferring ID
def get_channel_from_category(category:discord.CategoryChannel, name:str, chan_id:int) -> discord.abc.GuildChannel:
    '''Get a channel by ID or name, preferring ID, given a Guild'''
    chan_by_id = discord.utils.find(lambda c: (c.type == discord.ChannelType.voice or c.type == discord.ChannelType.text) and c.id == chan_id, category.channels)
    return chan_by_id or discord.utils.find(lambda c: (c.type == discord.ChannelType.voice or c.type == discord.ChannelType.text) and c.name == name, category.channels)

# Get a role by ID or name, preferring ID
def get_role(guild:discord.Guild, name:str, role_id:int) -> discord.Role:
    '''Get a role by ID or name, preferring ID, given a Guild'''
    role_by_id = discord.utils.find(lambda r: r.id == role_id, guild.roles)
    return role_by_id or discord.utils.find(lambda r: r.name==name, guild.roles)


def get_user_name(user:discord.User) -> str:
    '''Get a nice-looking name for a discord user, ignoring any (ST) prefix'''
    name = user.display_name
    # But ignore the starting (ST) for storytellers
    if name.startswith('(ST) '):
        name = name[5:]

    return name

# Given a list of users, return a list of their names
def user_names(users:list[discord.User]) -> list[str]:
    '''Get a list of nice-looking names from a list of users'''
    return list(map(get_user_name, users))

def get_user_by_id(users:list[discord.User], id:int) -> discord.User:
    '''Get a user by ID from a list'''
    for check in users:
        if check.id == id:
            return check
    return None

# Given a list of users and a name string, find the user with the closest name
def get_closest_user(user_list:list[discord.User], name:str) -> discord.User:
    '''Get anybody whose name starts with what was sent'''
    for user in user_list:
        # See if anybody's name starts with what was sent
        uname = get_user_name(user).lower()

        if uname.startswith(name.lower()):
            return user

    return None
