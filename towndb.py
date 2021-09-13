import discord
from discord.ext import commands

import botctypes

class TownDb:

    def __init__(self, mongo):
        self.collection = mongo['GuildInfo']

    def get_town_info(self, ctx:commands.Context) -> botctypes.TownInfo:
        return self.get_town_info_by_ids(ctx.guild.id, ctx.channel.id, ctx.guild)

    def get_town_info_by_town_id(self, town_id:botctypes.TownId, guild:discord.Guild) -> botctypes.TownInfo:
        return self.get_town_info_by_ids(town_id.guild_id, town_id.channel_id, guild)

    def get_town_info_by_ids(self, guild_id:int, channel_id:int, guild:discord.Guild) -> botctypes.TownInfo:
        query = { "guild" : guild_id, "controlChannelId" : channel_id }
        doc = self.collection.find_one(query)

        if doc:
            return botctypes.TownInfo.create_from_document(guild=guild, document=doc)
        return None

