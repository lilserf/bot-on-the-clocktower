import discord
from discord.ext import commands

import botctypes

class TownDb:
    '''Class wrapping the DB storage for Towns'''

    def __init__(self, mongo):
        self.collection = mongo['GuildInfo']

    def get_town_info(self, ctx:commands.Context) -> botctypes.TownInfo:
        '''Get a TownInfo from a given message Context'''
        return self.get_town_info_by_ids(ctx.guild.id, ctx.channel.id, ctx.guild)

    def get_town_info_by_town_id(self, town_id:botctypes.TownId, guild:discord.Guild) -> botctypes.TownInfo:
        '''Get a TownInfo given a TownId struct'''
        return self.get_town_info_by_ids(town_id.guild_id, town_id.channel_id, guild)

    def get_town_info_by_ids(self, guild_id:int, channel_id:int, guild:discord.Guild) -> botctypes.TownInfo:
        '''Get a TownInfo given a guild id and control channel id'''
        query = { "guild" : guild_id, "controlChannelId" : channel_id }
        doc = self.collection.find_one(query)

        if doc:
            return botctypes.TownInfo.create_from_document(guild=guild, document=doc)
        return None

    def is_channel_valid(self, guild:discord.Guild, chan:discord.abc.GuildChannel) -> bool:
        '''Is this channel a valid control channel for this guild?'''
        if isinstance(chan, discord.DMChannel):
            return False

        query = { "guild" : guild.id }
        result = self.collection.find(query)
        chan_ids = map(lambda x: x["controlChannelId"], result)

        if isinstance(chan, discord.TextChannel):
            if chan.id in chan_ids:
                return True

        return False

    @staticmethod
    def query(info:botctypes.TownInfo) -> dict:
        '''Common query used for many lookups'''
        return { "guild" : info.guild.id, "controlChannelId" : info.control_channel.id }

    def find_one_by_control_id(self, guild:discord.Guild, control_id:int) -> botctypes.TownInfo:
        '''Get a TownInfo from a guild and control channel id'''
        query = { "guild" : guild.id, "controlChannelId" : control_id }
        doc = self.collection.find_one(query)
        return botctypes.TownInfo.create_from_document(guild=guild, document=doc)

    def exists(self, info:botctypes.TownInfo) -> bool:
        '''Does this TownInfo exist in the DB?'''
        doc = self.collection.find_one(self.query(info))
        return doc is not None

    def update_one(self, info:botctypes.TownInfo) -> bool:
        '''Update the record in the DB for this TownInfo'''
        result = self.collection.replace_one(self.query(info), info.get_document(), True)
        return result.matched_count > 0

    def delete_one(self, info:botctypes.TownInfo) -> bool:
        '''Delete the record in the DB for this TownInfo'''
        result = self.collection.delete_one(self.query(info))
        return result.deleted_count > 0

    def delete_one_by_day_name(self, guild:discord.Guild, day_category_name:str) -> bool:
        '''Delete a town by the guild and day category name'''
        query = {"guild" : guild.id, "dayCategory" : day_category_name}
        result = self.collection.delete_one(query)
        return result.deleted_count > 0

    def delete_one_by_control(self, guild:discord.Guild, control:discord.TextChannel) -> bool:
        '''Delete a town by the guild and control channel'''
        query = {"guild" : guild.id, "controlChannelId" : control.id}
        result = self.collection.delete_one(query)
        return result.deleted_count > 0

    def get_all_guilds(self):
        '''Get all the distinct guilds known in the DB'''
        return self.collection.find(projection={'guild':True, '_id':False}).distinct('guild')

    def get_all_towns_for_guild_id(self, guild_id:int):
        '''Get all the DB records for a given guild'''
        query = {"guild": guild_id}
        return self.collection.find(query)
