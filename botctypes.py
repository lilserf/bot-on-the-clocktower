'''Module containing various utility types for Bot on the Clocktower'''
import datetime
import discord
import discordhelper

class TownId:
    '''Typed class for a guild ID and channel ID together'''
    def __init__(self, guild_id, channel_id):
        self.guild_id = guild_id
        self.channel_id = channel_id

    def __eq__(self, other):
        return other.guild_id == self.guild_id and other.channel_id == self.channel_id

    def __hash__(self):
        return hash(tuple((self.channel_id, self.guild_id)))

# Nicer than a dict for storing info about the town
class TownInfo:
    # pylint: disable=invalid-name
    # TODO: Fix the non-snake-case names in here
    '''Utility class encapsulating all the Stuff that is important to a Town'''
    guild:discord.Guild
    dayCategory:discord.CategoryChannel
    nightCategory:discord.CategoryChannel
    townSquare:discord.VoiceChannel
    controlChannel:discord.TextChannel
    dayChannels:list[discord.VoiceChannel]
    nightChannels:list[discord.VoiceChannel]
    chatChannel:discord.TextChannel
    storyTellerRole:discord.Role
    villagerRole:discord.Role
    activePlayers:set[discord.Member]
    storyTellers:set[discord.Member]
    villagers:set[discord.Member]
    authorName:str
    timestamp:datetime.datetime

    def __init__(self, guild, document):

        if document:
            self.guild = guild

            self.dayCategory = discordhelper.get_category(guild, document["dayCategory"], document["dayCategoryId"])
            self.nightCategory = document["nightCategory"] and discordhelper.get_category(guild, document["nightCategory"], document["nightCategoryId"]) or None

            self.townSquare = discordhelper.get_channel_from_category(self.dayCategory, document["townSquare"], document["townSquareId"])
            self.controlChannel = discordhelper.get_channel_from_category(self.dayCategory, document["controlChannel"], document["controlChannelId"])

            self.dayChannels = list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id ==  self.dayCategory.id)
            self.nightChannels = self.nightCategory and list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id ==  self.nightCategory.id) or []

            self.storyTellerRole = discordhelper.get_role(guild, document["storyTellerRole"], document["storyTellerRoleId"])
            self.villagerRole = discordhelper.get_role(guild, document["villagerRole"], document["villagerRoleId"])

            self.chatChannel = None
            if 'chatChannel' in document and 'chatChannelId' in document:
                self.chatChannel = discordhelper.get_channel_from_category(self.dayCategory, document['chatChannel'], document['chatChannelId'])

            activePlayers = set()
            for c in self.dayChannels:
                activePlayers.update(c.members)
            for c in self.nightChannels:
                activePlayers.update(c.members)
            self.activePlayers = activePlayers

            self.storyTellers = set()
            self.villagers = set()
            for p in self.activePlayers:
                if self.storyTellerRole in p.roles:
                    self.storyTellers.add(p)
                else:
                    self.villagers.add(p)

            self.authorName = document["authorName"]
            self.timestamp = document["timestamp"]

    def make_embed(self) -> discord.Embed:

        guild = self.guild
        embed = discord.Embed(title=f'{guild.name} // {self.dayCategory.name}', description=f'Created {self.timestamp} by {self.authorName}', color=0xcc0000)
        embed.add_field(name="Control Channel", value=self.controlChannel.name, inline=False)
        embed.add_field(name="Town Square", value=self.townSquare.name, inline=False)
        embed.add_field(name="Chat Channel", value=self.chatChannel and self.chatChannel.name or "<None>", inline=False)
        embed.add_field(name="Day Category", value=self.dayCategory.name, inline=False)
        embed.add_field(name="Night Category", value=self.nightCategory and self.nightCategory.name or "<None>", inline=False)
        embed.add_field(name="Storyteller Role", value=self.storyTellerRole.name, inline=False)
        embed.add_field(name="Villager Role", value=self.villagerRole.name, inline=False)
        return embed


