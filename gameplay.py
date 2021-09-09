import typing
import botctypes

# TODO
class IActivityRecorder():
    def record_activity(self, guild:discord.Guild, channel:discord.Channel) -> None:
        pass

# TODO
class ICleanup():
    def game_active() -> None:
        pass

class GameplayImpl():

    recorder:IActivityRecorder
    cleanup:ICleanup

    def __init__(recorder:IActivityRecorder, cleanup:ICleanup):
        self.recorder = recorder
        self.cleanup = cleanup

    async def end_game(self, info:TownInfo) -> string:
        pass

    async def set_storytellers(self, info:TownInfo, sts:list[discord.Member]) -> string:
        pass

    async def current_game(self, info:TownInfo) -> string:

        messages = []

        # find all guild members with the Current Game role
        prevPlayers = set()
        for m in guild.members:
            if info.villagerRole in m.roles:
                prevPlayers.add(m)

        # grant the storyteller the Current Storyteller role if necessary
        storyTeller = ctx.message.author
        if storyTeller not in info.storyTellers:
            messages.append(f"New storyteller: **{self.getUserName(storyTeller)}**. (Use `{COMMAND_PREFIX}setStorytellers` for 2+ storytellers)")
            await self.set_storytellers([storyTeller])

        # find additions and deletions by diffing the sets
        remove = prevPlayers - info.activePlayers
        add = info.activePlayers - prevPlayers

        # remove any stale players
        if len(remove) > 0:
            removeMsg = f"Removed **{info.villagerRole.name} role from: **"
            removeMsg += ', '.join(self.userNames(remove))
            removeMsg += "**"
            for m in remove:
                await m.remove_roles(info.villagerRole)
            messages.append(removeMsg)

        # add any new players
        if len(add) > 0:
            addMsg = f"Added **{info.villagerRole.name}** role to: **"
            addMsg += ', '.join(self.userNames(add))
            addMsg += "**"
            for m in add:
                await m.add_roles(info.villagerRole)
            messages.append(addMsg)

        self.recorder.record_activity(info.guild, info.controlChannel)

        self.cleanup.game_active()



    async def phase_night(self, info:TownInfo) -> string:
        pass

    async def phase_day(self, info:TownInfo) -> string:
        pass

    async def phase_vote(self, info:TownInfo) -> string:
        pass