# pylint: disable=missing-module-docstring, missing-class-docstring, missing-function-docstring
import discord

from botctypes import TownInfo
import discordhelper

class SetupImpl:

    def __init__(self, *, db, command_prefix:str): # pylint: disable=invalid-name
        self.collection = db['GuildInfo']
        self.command_prefix = command_prefix

    # Add a town from an ordered list of params, NOT including any !addTown command or similar
    def add_town_from_params(self, *, guild:discord.Guild, params:list[str], author:discord.User) -> str:
        # pylint: disable=too-many-locals, too-many-branches
        control_name = None
        town_square_name = None
        day_cat_name = None
        night_cat_name = None
        st_role_name = None
        villager_role_name = None
        chat_channel_name = None

        has_named_args = False

        for _, item in enumerate(params):
            split_arg = item.split("=")
            if len(split_arg) != 2:
                has_named_args = False
                break

            has_named_args = True
            arg_name = split_arg[0].lower()
            arg_value = split_arg[1]

            if arg_name == "control":
                control_name = arg_value
            elif arg_name == "townsquare":
                town_square_name = arg_value
            elif arg_name == "daycategory":
                day_cat_name = arg_value
            elif arg_name == "nightcategory":
                night_cat_name = arg_value
            elif arg_name == "strole":
                st_role_name = arg_value
            elif arg_name == "villagerrole":
                villager_role_name = arg_value
            elif arg_name == "chatchannel":
                chat_channel_name = arg_value
            else:
                return f'Unknown param to `{self.command_prefix}addTown`: \"{arg_name}\". Valid params: control, townSquare, dayCategory, nightCategory, stRole, villagerRole, chat_channel'

        if not has_named_args:
            if len(params) < 6:
                return f'Too few params to `{self.command_prefix}addTown`: should provide `<control channel> <townsquare channel> <day category> <night category> <storyteller role> <villager role> [chat channel]`'

            control_name = params[0]
            town_square_name = params[1]
            day_cat_name = params[2]
            night_cat_name = params[3]
            st_role_name = params[4]
            villager_role_name = params[5]
            if len(params) > 6:
                chat_channel_name = params[6]

        (info, error) = TownInfo.create_from_params(guild=guild, control_name=control_name, town_square_name=town_square_name, day_category_name=day_cat_name, night_category_name=night_cat_name, \
            storyteller_role_name=st_role_name, villager_role_name=villager_role_name, chat_channel_name=chat_channel_name, author=author)

        if not info:
            return error

        return self.add_town(info)

    # Add a town from a TownInfo
    def add_town(self, info:TownInfo) -> str:

        msg = None
        # Check if a town already exists
        query = {
            "guild" : info.guild.id,
            "dayCategoryId" : info.day_category.id
        }

        existing = self.collection.find_one(query)
        if existing:
            msg = f'Found an existing town on this server using daytime category `{info.day_category.name}`, modifying it!'

        # Upsert the town into place
        night_cat_info = (f'night category [{info.night_category.id}]' if info.night_category else '<no night category>')
        print(f'Adding a town to guild {info.guild.id} with control channel [{info.control_channel.id}], day category [{info.day_category.id}], {night_cat_info}')

        self.collection.replace_one(query, info.get_document(), True)

        return msg

    # Remove a town given a guild and either a control channel or day category name
    def remove_town(self, *, guild:discord.Guild, control_channel:discord.TextChannel=None, day_category_name:str=None) -> str:

        post = {}
        msg = None
        if control_channel is not None:
            post = {"guild" : guild.id, "controlChannelId" : control_channel.id }
            msg = f'control channel "{control_channel.name}"'
            print(f'Removing a game from guild {post["guild"]} with control channel [{post["controlChannelId"]}]')
        elif day_category_name is not None:
            post = {"guild" : guild.id, "dayCategory" : day_category_name}
            msg = f'day category "{day_category_name}"'
            print(f'Removing a game from guild {post["guild"]} with day category [{post["dayCategory"]}]')
        else:
            return 'Must provide either a control channel or day category to remove a town.'

        result = self.collection.delete_one(post)

        if result.deleted_count > 0:
            return None
        return f"Couldn't find a town to remove with {msg}!"

    def set_chat_channel(self, info:TownInfo, chat_channel_name:str) -> str:

        query = { "guild" : info.guild.id, "controlChannelId" : info.control_channel.id }
        doc = self.collection.find_one(query)
        if not doc:
            return 'Could not find a town! Are you running this command from the town control channel?'

        day_category:discord.CategoryChannel = discordhelper.get_category(info.guild, doc['dayCategory'], doc['dayCategoryId'])
        if not day_category:
            return f'Could not find the category {doc["dayCategory"]}!'

        chat_channel:discord.TextChannel = discordhelper.get_channel_from_category_by_name(day_category, chat_channel_name)
        if not chat_channel:
            return f'Could not find the channel {chat_channel_name} in the category {doc["dayCategory"]}!'

        doc['chat_channel'] = chat_channel.name
        doc['chatChannelId'] = chat_channel.id
        self.collection.replace_one(query, doc, True)

        return None

    FMT_DAY_CAT = '{town_name}'
    FMT_NIGHT_CAT = '{town_name} - Night'
    FMT_ST_ROLE = '{town_name} Storyteller'
    FMT_VILLAGER_ROLE = '{town_name} Villager'

    CONTROL_CHAN = 'botc_control'
    CHAT_CHAN = 'chat'
    TOWN_SQUARE_CHAN = 'Town Square'
    EXTRA_DAY_CHANS = ["Dark Alley", "Library", "Graveyard"]
    NIGHT_CHAN = 'Cottage'
    NUM_NIGHT_CHANS = 20


    async def create_town(self, *, guild:discord.Guild, town_name:str, allow_night_category:bool=True, guild_st_role:discord.Role=None, \
        guild_player_role:discord.Role=None, author:discord.User=None, bot_role:discord.Role) -> (TownInfo, str):
        # pylint: disable=too-many-locals, too-many-branches, too-many-statements

        game_villager_role_name = self.FMT_VILLAGER_ROLE.format(town_name=town_name)
        game_st_role_name = self.FMT_ST_ROLE.format(town_name=town_name)
        day_cat_name = self.FMT_DAY_CAT.format(town_name=town_name)
        night_cat_name = self.FMT_NIGHT_CAT.format(town_name=town_name)
        control_channel_name = self.CONTROL_CHAN
        chat_channel_name = self.CHAT_CHAN
        town_square_channel_name = self.TOWN_SQUARE_CHAN
        extra_channel_names = self.EXTRA_DAY_CHANS
        night_channel_name = self.NIGHT_CHAN
        needed_night_channels = self.NUM_NIGHT_CHANS

        # Roles
        everyone_role = discordhelper.get_role_by_name(guild, "@everyone")
        if not everyone_role:
            return (None, "Could not find the **@everyone** role. Why not?")

        game_villager_role = discordhelper.get_role_by_name(guild, game_villager_role_name)
        if not game_villager_role:
            game_villager_role = await guild.create_role(name=game_villager_role_name, color=discord.Color.magenta())

        game_st_role = discordhelper.get_role_by_name(guild, game_st_role_name)
        if not game_st_role:
            game_st_role = await guild.create_role(name=game_st_role_name, color=discord.Color.dark_magenta())

        # Day category
        day_cat = discordhelper.get_category_by_name(guild, day_cat_name)
        if not day_cat:
            day_cat = await guild.create_category(day_cat_name)

        await day_cat.set_permissions(game_villager_role, view_channel=True)
        await day_cat.set_permissions(bot_role, view_channel=True, move_members=True)

        # Night category
        if allow_night_category:
            night_cat = discordhelper.get_category_by_name(guild, night_cat_name)
            if not night_cat:
                night_cat = await guild.create_category(night_cat_name)

            await night_cat.set_permissions(game_st_role, view_channel=True)
            await night_cat.set_permissions(bot_role, view_channel=True, move_members=True)
            await night_cat.set_permissions(everyone_role, view_channel=False)

        # Mover channel
        control_channel = discordhelper.get_channel_from_category_by_name(day_cat, control_channel_name)
        if not control_channel:
            control_channel = await day_cat.create_text_channel(control_channel_name)
        await control_channel.set_permissions(bot_role, view_channel=True)
        await control_channel.set_permissions(game_villager_role, overwrite=None)

        if guild_st_role:
            await control_channel.set_permissions(guild_st_role, view_channel=True)
            await control_channel.set_permissions(everyone_role, view_channel=False)

        # Chat channel
        chat_channel = discordhelper.get_channel_from_category_by_name(day_cat, chat_channel_name)
        if not chat_channel:
            chat_channel = await day_cat.create_text_channel(chat_channel_name)
        await chat_channel.set_permissions(bot_role, view_channel=True)
        if not guild_player_role:
            await chat_channel.set_permissions(everyone_role, view_channel=False)


        # Town Square
        town_square_channel = discordhelper.get_channel_from_category_by_name(day_cat, town_square_channel_name)
        if not town_square_channel:
            town_square_channel = await day_cat.create_voice_channel(town_square_channel_name)

        if guild_player_role:
            await day_cat.set_permissions(everyone_role, view_channel=False)
            await town_square_channel.set_permissions(guild_player_role, view_channel=True)

        # Extra day channels
        for extra_channel_name in extra_channel_names:
            extra_channel = discordhelper.get_channel_from_category_by_name(day_cat, extra_channel_name)
            if not extra_channel:
                extra_channel = await day_cat.create_voice_channel(extra_channel_name)
            if not guild_player_role:
                await extra_channel.set_permissions(everyone_role, view_channel=False)


        # Night channels
        if allow_night_category:
            for chan in night_cat.channels:
                if chan.type == discord.ChannelType.voice and chan.name == night_channel_name:
                    needed_night_channels = needed_night_channels - 1

            if needed_night_channels > 0:
                for _ in range(needed_night_channels):
                    await night_cat.create_voice_channel(night_channel_name)


        # Calling addTown
        (info, err) = TownInfo.create_from_params(guild=guild, control_name=control_channel_name, town_square_name=town_square_channel_name, \
            day_category_name=day_cat_name, night_category_name=night_cat_name, storyteller_role_name=game_st_role_name, villager_role_name=game_villager_role_name, \
            chat_channel_name=chat_channel_name, author=author)

        if not info:
            return (None, f'There was a problem creating the town of **{town_name}**:\n{err}')

        self.add_town(info)
        return (info, None)

