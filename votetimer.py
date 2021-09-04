import datetime
from discord.ext import tasks
import pytimeparse
import shlex

class VoteTownInfo:
    def __init__(self, chat_channel, villager_role):
        self.chat_channel = chat_channel
        self.villager_role = villager_role


class VoteTimerCountdown:
    def __init__(self, town_storage, town_ticker, message_broadcaster):
        self.town_storage = town_storage
        self.town_ticker = town_ticker
        self.message_broadcaster = message_broadcaster

        self.town_ticker.set_callback(self.on_timer_complete)

    def add_town(self, town_info, seconds):
        pass

    async def on_timer_complete(town_info):
        pass


class VoteTownTickerVirtual:
    def set_callback(self, cb):
        pass

    def start_ticking(self):
        pass

    def stop_ticking(self):
        pass


class MessageBroadcasterVirtual:
    def send_message(self, message):
        pass


class VoteTownTickerConcrete(VoteTownTickerVirtual):
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



class VoteTownStorageVirtual:
    def add_town(self, town_info, ticks):
        pass

    def remove_town(self, town_info):
        pass

    def tick_and_return_towns(self):
        pass

    def has_towns_ticking(self):
        pass


class VoteTownStorageConcrete(VoteTownStorageVirtual):
    def __init__(self):
        self.ticking_towns = {}

    def add_town(self, town_info, ticks):
        self.ticking_towns[town_info] = ticks

    def remove_town(self, town_info):
        if town_info in self.ticking_towns:
            self.ticking_towns.pop(town_info)
        pass

    def tick_and_return_towns(self):
        ret = []
        keys = self.ticking_towns.keys()
        for key in keys:
            ticks = self.ticking_towns[key] - 1
            if ticks <= 0:
                ret.append(key)
            else:
                self.ticking_towns[key] = ticks
        for key in ret:
            self.ticking_towns.pop(key)
        return ret

    def has_towns_ticking(self):
        return self.ticking_towns


class VoteTownInfoProviderVirtual:
    def get_town_info(self):
        pass

class VoteTownInfoProviderConcrete(VoteTownInfoProviderVirtual):
    def __init__(self, bot, ctx):
        self.bot = bot
        self.ctx = ctx

    def get_town_info(self):
        town_info = self.bot.getTownInfo(self.ctx)
        if not town_info:
            return None

        return VoteTownInfo(town_info.chatChannel, town_info.villagerRole)

class VoteTimerImpl:
    def __init__(self):
        pass

    async def start_timer(self, town_info_provider, time_in_seconds):
        town_info = town_info_provider.get_town_info()

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

        return None

    def get_seconds_from_string(self, str):
        return pytimeparse.parse(str)

# Concrete class for use by the Cog
class VoteTimer:
    def __init__(self, bot):
        self.impl = VoteTimerImpl()
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

        town_info_provider = VoteTownInfoProviderConcrete(self.bot, ctx)

        return f'TEMP time in seconds passed: {time_seconds}'

        #return await self.impl.start_timer(server_token, self.find_role_from_message_content(ctx.message.content))

    async def stop_timer(self, ctx):
        return f'TEMP okay we should stop that timer'