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
    '''Utility class encapsulating all the Stuff that is important to a Town'''
    guild:discord.Guild
    day_category:discord.CategoryChannel
    night_category:discord.CategoryChannel
    town_square_channel:discord.VoiceChannel
    control_channel:discord.TextChannel
    day_channels:list[discord.VoiceChannel]
    night_channels:list[discord.VoiceChannel]
    chat_channel:discord.TextChannel
    storyteller_role:discord.Role
    villager_role:discord.Role
    active_players:set[discord.Member]
    storytellers:set[discord.Member]
    villagers:set[discord.Member]
    author:discord.Member
    timestamp:datetime.datetime

    def __init__(self, *, guild:discord.Guild, day_category:discord.CategoryChannel, night_category:discord.CategoryChannel, town_square_channel:discord.VoiceChannel, \
        control_channel:discord.TextChannel, chat_channel:discord.TextChannel=None, storyteller_role:discord.Role, villager_role:discord.Role, author:discord.Member=None, \
        timestamp:datetime.datetime=datetime.datetime.now()):
        '''Constructor which takes incoming discord objects and derives the other fields it should have'''
        # pylint: disable=consider-using-ternary, invalid-name

        self.guild = guild
        self.day_category = day_category
        self.night_category = night_category
        self.town_square_channel = town_square_channel
        self.control_channel = control_channel
        self.chat_channel = chat_channel
        self.storyteller_role = storyteller_role
        self.villager_role = villager_role
        self.author = author
        self.timestamp = timestamp

        self.day_channels = list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id ==  self.day_category.id)
        self.night_channels = self.night_category and list(c for c in guild.channels if c.type == discord.ChannelType.voice and c.category_id ==  self.night_category.id) or []

        self.active_players = set()
        for c in self.day_channels:
            self.active_players.update(c.members)
        for c in self.night_channels:
            self.active_players.update(c.members)

        self.storytellers = set()
        self.villagers = set()
        for p in self.active_players:
            if self.storyteller_role in p.roles:
                self.storytellers.add(p)
            else:
                self.villagers.add(p)

    def get_document(self) -> dict:
        '''Get a MongoDB-like document describing this Town'''
        doc = {
            "guild" : self.guild.id,
            "controlChannel" : self.control_channel.name,
            "controlChannelId" : self.control_channel.id,
            "chatChannel" : self.chat_channel.name if self.chat_channel else None,
            "chatChannelId" : self.chat_channel.id if self.chat_channel else None,
            "townSquare" : self.town_square_channel.name,
            "townSquareId" : self.town_square_channel.id,
            "dayCategory" : self.day_category.name,
            "dayCategoryId" : self.day_category.id,
            "nightCategory" : self.night_category.name if self.night_category else None,
            "nightCategoryId" : self.night_category.id if self.night_category else None,
            "storyTellerRole" : self.storyteller_role.name,
            "storyTellerRoleId" : self.storyteller_role.id,
            "villagerRole" : self.villager_role.name,
            "villagerRoleId" : self.villager_role.id,
            "authorName" : self.author.display_name if self.author else None,
            "author" : self.author.id if self.author else None,
            "timestamp" : self.timestamp
        }

        return doc

    @staticmethod
    def create_from_document(*, guild:discord.Guild, document) -> 'TownInfo':
        '''Create a TownInfo from a MongoDB-like document object'''
        if document:

            day_category:discord.CategoryChannel = discordhelper.get_category(guild, document["dayCategory"], document["dayCategoryId"])
            night_category:discord.CategoryChannel = document["nightCategory"] and discordhelper.get_category(guild, document["nightCategory"], document["nightCategoryId"]) or None

            town_square_channel:discord.VoiceChannel = discordhelper.get_channel_from_category(day_category, document["townSquare"], document["townSquareId"])
            control_channel:discord.TextChannel = discordhelper.get_channel_from_category(day_category, document["controlChannel"], document["controlChannelId"])

            storyteller_role:discord.Role = discordhelper.get_role(guild, document["storyTellerRole"], document["storyTellerRoleId"])
            villager_role:discord.Role = discordhelper.get_role(guild, document["villagerRole"], document["villagerRoleId"])

            chat_channel:discord.TextChannel = None
            if 'chatChannel' in document or 'chatChannelId' in document:
                chat_channel = discordhelper.get_channel_from_category(day_category, document['chatChannel'], document['chatChannelId'])

            author:discord.User = None
            if 'author' in document:
                author = discordhelper.get_user_by_id(guild.members, document["author"])
            elif 'authorName' in document:
                author = discordhelper.get_closest_user(guild.members, document["authorName"])

            timestamp = document["timestamp"]

            info = TownInfo(guild=guild, day_category=day_category, night_category=night_category, town_square_channel=town_square_channel, control_channel=control_channel, \
                chat_channel = chat_channel, storyteller_role = storyteller_role, villager_role=villager_role, author=author, timestamp=timestamp)

            return info

        return None

    @staticmethod
    def create_from_params(*, guild:discord.Guild, control_name:str, town_square_name:str, day_category_name:str, night_category_name:str, \
        storyteller_role_name:str, villager_role_name:str, chat_channel_name:str, author:discord.User=None) -> ('TownInfo', str):
        # pylint: disable=too-many-locals, too-many-return-statements
        '''Create a TownInfo from a set of strings naming channels and roles'''

        day_cat:discord.CategoryChannel = discordhelper.get_category_by_name(guild, day_category_name)
        night_cat:discord.CategoryChannel = night_category_name and discordhelper.get_category_by_name(guild, night_category_name) or None

        control_chan:discord.TextChannel = discordhelper.get_channel_from_category_by_name(day_cat, control_name)
        town_square_channel:discord.VoiceChannel = discordhelper.get_channel_from_category_by_name(day_cat, town_square_name)

        storyteller_role:discord.Role = discordhelper.get_role_by_name(guild, storyteller_role_name)
        villager_role:discord.Role = discordhelper.get_role_by_name(guild, villager_role_name)

        chat_channel:discord.TextChannel = None
        if chat_channel_name:
            chat_channel = discordhelper.get_channel_from_category_by_name(day_cat, chat_channel_name)

        # TODO: more checks
        # does the night category contain some channels to distribute people to?

        # TODO: also specify the roles to set
        # does the storyteller role have perms to see all the cottages?
        # do the player roles NOT have perms to see the cottages

        if not day_cat:
            return (None, f'Couldn\'t find a category named `{day_category_name}`!')

        if not control_chan:
            return (None, f'Couldn\'t find a channel named `{control_name}` in category `{day_category_name}`!')

        if chat_channel_name and not chat_channel:
            return (None, f'Couldn\'t find a chat channel named `{chat_channel_name}` in category `{day_category_name}`!')

        if not town_square_channel:
            return (None, f'Couldn\'t find a channel named `{town_square_name}` in category `{day_category_name}`!')

        if not storyteller_role:
            return (None, f'Couldn\'t find a role named `{storyteller_role_name}`!')

        if not villager_role:
            return (None, f'Couldn\'t find a role named `{villager_role_name}`!')

        info = TownInfo(guild=guild, day_category=day_cat, night_category=night_cat, town_square_channel=town_square_channel, control_channel=control_chan, \
            chat_channel=chat_channel, storyteller_role=storyteller_role, villager_role=villager_role, author=author)

        return (info, '')


    def make_embed(self) -> discord.Embed:
        '''Return a discord.Embed describing this Town'''
        guild = self.guild
        embed = discord.Embed(title=f'{guild.name} // {self.day_category.name}', description=f'Created {self.timestamp} by {self.author.display_name if self.author else "Unknown"}', color=0xcc0000)
        embed.add_field(name="Control Channel", value=self.control_channel.name, inline=False)
        embed.add_field(name="Town Square", value=self.town_square_channel.name, inline=False)
        embed.add_field(name="Chat Channel", value=self.chat_channel and self.chat_channel.name or "<None>", inline=False)
        embed.add_field(name="Day Category", value=self.day_category.name, inline=False)
        embed.add_field(name="Night Category", value=self.night_category and self.night_category.name or "<None>", inline=False)
        embed.add_field(name="Storyteller Role", value=self.storyteller_role.name, inline=False)
        embed.add_field(name="Villager Role", value=self.villager_role.name, inline=False)
        return embed


