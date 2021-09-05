import datetime
from discord.ext import tasks
import pytimeparse
import shlex

class VoteTownInfo:
    def __init__(self, chat_channel, villager_role):
        self.chat_channel = chat_channel
        self.villager_role = villager_role


class VoteTownId:
    def __init__(self, guild_id, channel_id):
        self.guild_id = guild_id
        self.channel_id = channel_id


class VoteTimerCountdown:
    def __init__(self, town_storage, town_ticker, message_broadcaster):
        self.town_storage = town_storage
        self.town_ticker = town_ticker
        self.message_broadcaster = message_broadcaster

        self.town_ticker.set_callback(self.on_timer_complete)

    def add_town(self, town_id, end_time):
        pass

    async def on_timer_complete(town_id):
        pass



class IDateTimeProvider:
    def now(self):
        pass

class DateTimeProvider(IDateTimeProvider):
    def now(self):
        return datetime.now()


class IVoteTownTicker:
    def set_callback(self, cb):
        pass

    def start_ticking(self):
        pass

    def stop_ticking(self):
        pass


class IMessageBroadcaster:
    def send_message(self, message):
        pass


class VoteTownTicker(IVoteTownTicker):
    def __del__(self):
        self.tick.cancel()

    def set_callback(self, cb):
        self.cb = cb

    def start_ticking(self):
        if not self.tick.is_running():
            self.tick.start()

    def stop_ticking(self):
        if not self.tick.is_running():
            self.tick.stop()

    @tasks.loop(seconds=1)
    async def tick(self):
        await self.cb()



class IVoteTownStorage:
    def add_town(self, town_id, finish_time):
        pass

    def remove_town(self, town_id):
        pass

    def tick_and_return_finished_towns(self):
        pass

    def has_towns_ticking(self):
        pass


class VoteTownStorage(IVoteTownStorage):
    def __init__(self, datetime_provider):
        self.ticking_towns = {}
        self.datetime_provider = datetime_provider

    def add_town(self, town_id, finish_time):
        self.ticking_towns[town_id] = finish_time

    def remove_town(self, town_id):
        if town_id in self.ticking_towns:
            self.ticking_towns.pop(town_id)
        pass

    def tick_and_return_finished_towns(self):
        ret = []
        keys = self.ticking_towns.keys()
        now = self.datetime_provider.now()
        for key in keys:
            if now >= self.ticking_towns[key]:
                ret.append(key)
        for key in ret:
            self.ticking_towns.pop(key)
        return ret

    def has_towns_ticking(self):
        return self.ticking_towns


class IVoteTownInfoProvider:
    def get_town_info(self, town_id):
        pass

class VoteTownInfoProvider(IVoteTownInfoProvider):
    def __init__(self, bot):
        self.bot = bot

    def get_town_info(self, town_id):
        town_info = self.bot.getTownInfoByIds(town_id.guild_id, town_id.channel_id)
        if not town_info:
            return None

        return VoteTownInfo(town_info.chatChannel, town_info.villagerRole)

class VoteTimerImpl:
    def __init__(self, town_info_provider):
        self.town_info_provider = town_info_provider
        pass

    async def start_timer(self, town_id, time_in_seconds):
        town_info = self.town_info_provider.get_town_info(town_id)

        if not town_info:
            return 'No town found here. Are you in a town control channel added via the "addTown" or "createTown" commands?'

        if not town_info.chat_channel:
            return 'No chat channel found for this town. Please set the chat channel via the "setChatChannel" command.'

        if not town_info.villager_role:
            return 'No villager role found for this town. Please set up the town properly via the "addTown" command.'

        required_time_str = 'Please choose a time between 10 seconds and 20 minutes.'

        if time_in_seconds < 10:
            return required_time_str

        if time_in_seconds > 1200:
            return required_time_str

        # TODO more stuff
        return None

    def get_seconds_from_string(self, str):
        return pytimeparse.parse(str)

# Concrete class for use by the Cog
class VoteTimer:
    def __init__(self, bot):
        self.impl = VoteTimerImpl(VoteTownInfoProvider(bot))
        self.bot = bot

    async def start_timer(self, ctx):
        params = shlex.split(ctx.message.content)
        
        usage = f'Must pass a time string, e.g. "5 minutes 30 seconds" or "5m30s"'

        if len(params) < 2:
            return usage

        time_string = ' '.join(params[1:])
        time_seconds = self.impl.get_seconds_from_string(time_string)
        if not time_seconds:
            return usage

        return f'TEMP time in seconds passed: {time_seconds}'

        town_id = VoteTownId(ctx.guild.id, cts.channel.id)

        return await self.impl.start_timer(town_id, time_seconds)

    async def stop_timer(self, ctx):
        return f'TEMP okay we should stop that timer'