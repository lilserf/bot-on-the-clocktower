'''Abstraction of Bot on the Clocktower gameplay into a module with minimal bot-specific dependencies'''
import discord
import discordhelper
from botctypes import TownInfo

# TODO
class IActivityRecorder():
    '''Interface for a class to record game activity'''
    def record_activity(self, guild:discord.Guild, channel:discord.abc.GuildChannel) -> None:
        '''Record that activity has happen on a given guild and channel'''

# TODO
class ICleanup():
    '''Interface for a class to manage game cleanup'''
    def game_active(self) -> None:
        '''Record that a game is active and cleanup should happen later'''

class GameplayImpl():
    '''Implementation for a gameplay object'''

    recorder:IActivityRecorder
    cleanup:ICleanup
    command_prefix:str

    def __init__(self, recorder:IActivityRecorder, cleanup:ICleanup, command_prefix:str):
        self.recorder = recorder
        self.cleanup = cleanup
        self.command_prefix = command_prefix

    async def end_game(self, info:TownInfo) -> str:
        '''End a game'''

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

        # set up the new storytellers
        for story_teller in sts:
            if story_teller is None:
                continue

            await story_teller.add_roles(info.storyTellerRole)
            
            # add (ST) to the start of the current storyteller
            if not story_teller.display_name.startswith('(ST) '):
                try:
                    await story_teller.edit(nick=f"(ST) {story_teller.display_name}")
                except:
                    pass

        message = f"Set **{info.storyTellerRole.name}** role for: **"
        message += ', '.join(map(discordhelper.get_user_name, sts))
        message += "**"
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
            remove_msg = f"Removed **{info.villagerRole.name} role from: **"
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

        self.recorder.record_activity(info.guild, info.controlChannel)

        self.cleanup.game_active()

        return "\n".join(messages)

    async def phase_night(self, info:TownInfo) -> str:
        '''Transition to night (move players to Cottages)'''

    async def phase_day(self, info:TownInfo) -> str:
        '''Transition to day (move players to Town Square)'''

    async def phase_vote(self, info:TownInfo) -> str:
        '''Transition to voting (move players to Town Square)'''

