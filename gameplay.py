'''Abstraction of Bot on the Clocktower gameplay into a module with minimal bot-specific dependencies'''
# pylint: disable=bare-except, broad-except
import random
import discord
import discordhelper
from botctypes import TownInfo

class IGameActivity():
    '''Interface for a class to manage game activity'''

    def record_activity(self, guild:discord.Guild, channel:discord.abc.GuildChannel, storytellers:list[discord.Member], active_players:list[discord.Member]) -> None:
        '''Record that activity has happen on a given guild and channel'''

    def remove_active_game(self, guild:discord.Guild, control_chan:discord.abc.GuildChannel) -> None:
        '''Remove this game'''

class IGameCleanup():
    '''Interface for a class that has a timer that can be started and stopped to cleanup inactive games'''

    def start(self) -> None:
        '''start the timer'''

    def stop(self) -> None:
        ''' stop the timer'''

class GameplayImpl():
    '''Implementation for a gameplay object'''

    activity:IGameActivity
    command_prefix:str

    def __init__(self, activity:IGameActivity, command_prefix:str):
        self.activity = activity
        self.command_prefix = command_prefix

    async def end_game(self, info:TownInfo) -> str:
        '''End a game'''
        guild = info.guild
        msg = ""

        # find all guild members with the Current Game role
        prev_players = set()
        prev_sts = set()
        for mem in guild.members:
            if info.villager_role in mem.roles:
                prev_players.add(mem)
            if info.storyteller_role in mem.roles:
                prev_sts.add(mem)

        name_list = ", ".join(discordhelper.user_names(prev_players))
        msg += f"Removed **{info.villager_role.name}** role from: **{name_list}**"
        # remove game role from players
        for mem in prev_players:
            await mem.remove_roles(info.villager_role)

        # remove cottage permissions
        for chan in info.night_channels:
            # Take away permission overwrites for this cottage
            for mem in prev_players:
                await chan.set_permissions(mem, overwrite=None)
            for prev_st in prev_sts:
                await chan.set_permissions(prev_st, overwrite=None)

        name_list = ", ".join(discordhelper.user_names(prev_sts))
        msg += f"\nRemoved **{info.storyteller_role.name}** role from: **{name_list}**"

        for prev_st in prev_sts:
            # remove storyteller role and name from storyteller
            await prev_st.remove_roles(info.storyteller_role)
            if prev_st.display_name.startswith('(ST) '):
                newnick = prev_st.display_name[5:]
                try:
                    await prev_st.edit(nick=newnick)
                except:
                    pass

        self.activity.remove_active_game(info.guild, info.control_channel)
        return msg


    async def set_storytellers(self, info:TownInfo, sts:list[discord.Member]) -> str:
        '''Set the storytellers'''

        # take any (ST) off of old storytellers
        for old in info.storytellers:
            if old not in sts:
                await old.remove_roles(info.storyteller_role)
            if old not in sts and old.display_name.startswith('(ST) '):
                new_nick = old.display_name[5:]
                try:
                    await old.edit(nick=new_nick)
                except:
                    pass

        valid_sts = []
        # set up the new storytellers
        for story_teller in sts:
            if story_teller is None:
                continue

            valid_sts.append(discordhelper.get_user_name(story_teller))
            await story_teller.add_roles(info.storyteller_role)

            # add (ST) to the start of the current storyteller
            if not story_teller.display_name.startswith('(ST) '):
                try:
                    await story_teller.edit(nick=f"(ST) {story_teller.display_name}")
                except:
                    pass

        message:str = ""
        if len(valid_sts) > 0:
            message = f"Set **{info.storyteller_role.name}** role for: **"
            message += ', '.join(valid_sts)
            message += "**"
        else:
            message = 'No valid storytellers found!'
        return message

    async def current_game(self, info:TownInfo, author:discord.Member) -> str:
        '''Initiate a game in this town'''
        messages = []

        guild = info.guild

        # find all guild members with the Current Game role
        prev_players = set()
        for mem in guild.members:
            if info.villager_role in mem.roles:
                prev_players.add(mem)

        # grant the storyteller the Current Storyteller role if necessary
        story_teller = author
        if story_teller not in info.storytellers:
            messages.append(f"New storyteller: **{discordhelper.get_user_name(story_teller)}**. (Use `{self.command_prefix}setStorytellers` for 2+ storytellers)")
            await self.set_storytellers(info, [story_teller])

        # find additions and deletions by diffing the sets
        remove = prev_players - info.active_players
        add = info.active_players - prev_players

        # remove any stale players
        if len(remove) > 0:
            remove_msg = f"Removed **{info.villager_role.name}** role from: **"
            remove_msg += ', '.join(discordhelper.user_names(remove))
            remove_msg += "**"
            for mem in remove:
                await mem.remove_roles(info.villager_role)
            messages.append(remove_msg)

        # add any new players
        if len(add) > 0:
            add_msg = f"Added **{info.villager_role.name}** role to: **"
            add_msg += ', '.join(discordhelper.user_names(add))
            add_msg += "**"
            for mem in add:
                await mem.add_roles(info.villager_role)
            messages.append(add_msg)

        # TODO: need list of storytellers and activePlayers here, maybe should send a diff of what was in the DB before though
        self.activity.record_activity(info.guild, info.controlChannel, [], [])

        return "\n".join(messages)

    async def phase_night(self, info:TownInfo, author:discord.Member) -> str:
        '''Transition to night (move players to Cottages)'''
        # do role switching for active game
        await self.current_game(info, author)

        if not info.night_category:
            return f'This town does not have a Night category and therefore does not support the `{self.command_prefix}day` or `{self.command_prefix}night` commands. If you want to change this, please add a Night category and use the `!addTown` command to update the town info!'

        messages = []

        # get list of users in town square
        users = list(info.villagers)
        users.sort(key=lambda x: x.display_name.lower())
        cottages = list(info.night_channels)
        cottages.sort(key=lambda x: x.position)

        messages.append(f'Moving {len(info.storytellers)} storytellers and {len(users)} villagers to Cottages!')

        # Put all storytellers in the first cottage
        first_cottage = cottages[0]
        for steller in info.storytellers:
            await steller.move_to(first_cottage)

        # And everybody else in the rest
        cottages = cottages[1:]

        # pair up users with cottages
        pairs = list(map(lambda x, y: (x,y), users, cottages))
        # randomize the order people are moved
        random.shuffle(pairs)

        # move each user to a cottage
        for (user, cottage) in pairs:
            # grant the user permissions for their own cottage so they can see streams (if they're the Spy, for example)
            try:
                await cottage.set_permissions(user, view_channel=True)
            except Exception:
                # Well, we can still move people even if the permission changes fail
                pass
            await user.move_to(cottage)

        return '\n'.join(messages)

    async def phase_day(self, info:TownInfo) -> str:
        '''Transition to day (move players to Town Square)'''
        if not info.night_category:
            return f'This town does not have a Night category and therefore does not support the `{self.command_prefix}day` or `{self.command_prefix}night` commands. If you want to change this, please add a Night category and use the `!addTown` command to update the town info!'

        # get users in night channels
        users = []
        for chan in info.night_channels:
            users.extend(chan.members)
            # Take away permission overwrites for their cottage
            for mem in chan.members:
                await chan.set_permissions(mem, overwrite=None)

        msg = f'Moving {len(users)} players from Cottages to **{info.town_square_channel.name}**.'

        # randomize the order we bring people back
        random.shuffle(users)

        # move them to Town Square
        for user in users:
            await user.move_to(info.town_square_channel)

        return msg

    async def phase_vote(self, info:TownInfo) -> str:
        '''Transition to voting (move players to Town Square)'''

        # get users in day channels other than Town Square
        users = []
        for chan in info.day_channels:
            if chan != info.town_square_channel:
                users.extend(chan.members)

        msg = f'Moving {len(users)} players from daytime channels to **{info.town_square_channel.name}**.'

        # move them to Town Square
        for user in users:
            await user.move_to(info.town_square_channel)

        return msg

