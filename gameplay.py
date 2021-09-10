'''Abstraction of Bot on the Clocktower gameplay into a module with minimal bot-specific dependencies'''
import random
import discord
import discordhelper
from botctypes import TownInfo

class IGameActivity():
    '''Interface for a class to manage game activity'''

    def record_activity(self, guild:discord.Guild, channel:discord.abc.GuildChannel) -> None:
        '''Record that activity has happen on a given guild and channel'''

    def remove_active_game(self, guild:discord.Guild, channel:discord.abc.GuildChannel) -> None:
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
            if info.villagerRole in mem.roles:
                prev_players.add(mem)
            if info.storyTellerRole in mem.roles:
                prev_sts.add(mem)

        name_list = ", ".join(discordhelper.user_names(prev_players))
        msg += f"Removed **{info.villagerRole.name}** role from: **{name_list}**"
        # remove game role from players
        for mem in prev_players:
            await mem.remove_roles(info.villagerRole)

        # remove cottage permissions
        for chan in info.nightChannels:
            # Take away permission overwrites for this cottage
            for mem in prev_players:
                await chan.set_permissions(mem, overwrite=None)
            for prev_st in prev_sts:
                await chan.set_permissions(prev_st, overwrite=None)

        name_list = ", ".join(discordhelper.user_names(prev_sts))
        msg += f"\nRemoved **{info.storyTellerRole.name}** role from: **{name_list}**"

        for prev_st in prev_sts:
            # remove storyteller role and name from storyteller
            await prev_st.remove_roles(info.storyTellerRole)
            if prev_st.display_name.startswith('(ST) '):
                newnick = prev_st.display_name[5:]
                try:
                    await prev_st.edit(nick=newnick)
                except:
                    pass

        self.activity.remove_active_game(info.guild, info.controlChannel)
        return msg


    async def set_storytellers(self, info:TownInfo, sts:list[discord.Member]) -> str:
        '''Set the storytellers'''

        # take any (ST) off of old storytellers
        for old in info.storyTellers:
            if old not in sts:
                await old.remove_roles(info.storyTellerRole)
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
            await story_teller.add_roles(info.storyTellerRole)

            # add (ST) to the start of the current storyteller
            if not story_teller.display_name.startswith('(ST) '):
                try:
                    await story_teller.edit(nick=f"(ST) {story_teller.display_name}")
                except:
                    pass

        message:str = ""
        if len(valid_sts) > 0:
            message = f"Set **{info.storyTellerRole.name}** role for: **"
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
            if info.villagerRole in mem.roles:
                prev_players.add(mem)

        # grant the storyteller the Current Storyteller role if necessary
        story_teller = author
        if story_teller not in info.storyTellers:
            messages.append(f"New storyteller: **{discordhelper.get_user_name(story_teller)}**. (Use `{self.command_prefix}setStorytellers` for 2+ storytellers)")
            await self.set_storytellers(info, [story_teller])

        # find additions and deletions by diffing the sets
        remove = prev_players - info.activePlayers
        add = info.activePlayers - prev_players

        # remove any stale players
        if len(remove) > 0:
            remove_msg = f"Removed **{info.villagerRole.name}** role from: **"
            remove_msg += ', '.join(discordhelper.user_names(remove))
            remove_msg += "**"
            for mem in remove:
                await mem.remove_roles(info.villagerRole)
            messages.append(remove_msg)

        # add any new players
        if len(add) > 0:
            add_msg = f"Added **{info.villagerRole.name}** role to: **"
            add_msg += ', '.join(discordhelper.user_names(add))
            add_msg += "**"
            for mem in add:
                await mem.add_roles(info.villagerRole)
            messages.append(add_msg)

        self.activity.record_activity(info.guild, info.controlChannel)

        return "\n".join(messages)

    async def phase_night(self, info:TownInfo, author:discord.Member) -> str:
        '''Transition to night (move players to Cottages)'''
        # do role switching for active game
        await self.current_game(info, author)

        if not info.nightCategory:
            return f'This town does not have a Night category and therefore does not support the `{self.command_prefix}day` or `{self.command_prefix}night` commands. If you want to change this, please add a Night category and use the `!addTown` command to update the town info!'

        messages = []

        # get list of users in town square
        users = list(info.villagers)
        users.sort(key=lambda x: x.display_name.lower())
        cottages = list(info.nightChannels)
        cottages.sort(key=lambda x: x.position)

        messages.append(f'Moving {len(info.storyTellers)} storytellers and {len(users)} villagers to Cottages!')

        # Put all storytellers in the first cottage
        first_cottage = cottages[0]
        for steller in info.storyTellers:
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
        if not info.nightCategory:
            return f'This town does not have a Night category and therefore does not support the `{self.command_prefix}day` or `{self.command_prefix}night` commands. If you want to change this, please add a Night category and use the `!addTown` command to update the town info!'

        # get users in night channels
        users = []
        for chan in info.nightChannels:
            users.extend(chan.members)
            # Take away permission overwrites for their cottage
            for mem in chan.members:
                await chan.set_permissions(mem, overwrite=None)

        msg = f'Moving {len(users)} players from Cottages to **{info.townSquare.name}**.'

        # randomize the order we bring people back
        random.shuffle(users)

        # move them to Town Square
        for user in users:
            await user.move_to(info.townSquare)

        return msg

    async def phase_vote(self, info:TownInfo) -> str:
        '''Transition to voting (move players to Town Square)'''

        # get users in day channels other than Town Square
        users = []
        for chan in info.dayChannels:
            if chan != info.townSquare:
                users.extend(chan.members)

        msg = f'Moving {len(users)} players from daytime channels to **{info.townSquare.name}**.'

        # move them to Town Square
        for user in users:
            await user.move_to(info.townSquare)

        return msg

