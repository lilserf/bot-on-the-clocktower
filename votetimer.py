# pylint: disable=missing-module-docstring, missing-class-docstring, missing-function-docstring, broad-except
import datetime
import math
import shlex

from discord.ext import tasks
import pytimeparse

import botctypes

class VoteTownInfo:
    def __init__(self, chat_channel, villager_role, town_square_name):
        self.chat_channel = chat_channel
        self.villager_role = villager_role
        self.town_square_name = town_square_name

class IVoteTimerController:
    async def add_town(self, town_id, end_time):
        pass

    async def remove_town(self, town_id):
        pass

class VoteTimerController(IVoteTimerController):

    def __init__(self, datetime_provider, town_info_provider, town_storage, town_ticker, message_broadcaster, vote_handler):
        # pylint: disable=too-many-arguments
        self.datetime_provider = datetime_provider
        self.town_info_provider = town_info_provider
        self.town_storage = town_storage
        self.town_ticker = town_ticker
        self.message_broadcaster = message_broadcaster
        self.vote_handler = vote_handler

        self.town_ticker.set_callback(self.tick)

        self.town_map = {}

    async def add_town(self, town_id, end_time):
        self.town_map[town_id] = end_time
        now = self.datetime_provider.now()

        ret = await self.send_time_remaining_message(town_id, end_time, now)
        self.queue_next_time(town_id, end_time, now)
        self.town_ticker.start_ticking()
        return ret

    async def remove_town(self, town_id):
        had_town = False
        if town_id in self.town_map:
            had_town = True
            self.town_map.pop(town_id)
            self.town_storage.remove_town(town_id)

        if not self.town_storage.has_towns_ticking():
            self.town_ticker.stop_ticking()

        if had_town:
            town_info = self.town_info_provider.get_town_info(town_id)
            message = f'{town_info.villager_role.mention} - Vote countdown stopped!'
            await self.message_broadcaster.send_message(town_info, message)

    async def tick(self):
        finished = self.town_storage.tick_and_return_finished_towns()

        for town_id in finished:
            await self.advance_town(town_id, self.datetime_provider.now())

        if not self.town_storage.has_towns_ticking():
            self.town_ticker.stop_ticking()

    async def advance_town(self, town_id, now):
        if town_id in self.town_map:
            end_time = self.town_map[town_id]
            if end_time < now:
                self.town_map.pop(town_id)
                await self.send_time_to_vote_message(town_id)
                await self.vote_handler.perform_vote(town_id)
            else:
                await self.send_time_remaining_message(town_id, end_time, now)
                self.queue_next_time(town_id, end_time, now)

    async def send_time_remaining_message(self, town_id, end_time, now):
        town_info = self.town_info_provider.get_town_info(town_id)
        message = self.construct_message(town_info, end_time, now)
        return await self.message_broadcaster.send_message(town_info, message)

    async def send_time_to_vote_message(self, town_id):
        town_info = self.town_info_provider.get_town_info(town_id)
        return await self.message_broadcaster.send_message(town_info, f'{town_info.villager_role.mention} - Returning to {town_info.town_square_name} to vote!')

    def queue_next_time(self, town_id, end_time, now):
        next_time = end_time
        delta = (end_time - now).total_seconds()
        if delta >= 0:
            advance_times = [300, 60, 15, 0]
            for x in advance_times:
                if delta > x:
                    next_time = end_time - datetime.timedelta(seconds=x)
                    break

        self.town_storage.add_town(town_id, next_time)

    def construct_message(self, town_info, end_time, now):
        delta_seconds = (end_time - now).total_seconds()

        rounded_seconds = round(delta_seconds/5)*5

        message = f'{town_info.villager_role.mention} - '

        if rounded_seconds > 0:
            minutes = math.floor(rounded_seconds / 60)
            seconds = rounded_seconds % 60

            if minutes > 0:
                message = message + f'{minutes} minute'
                if minutes > 1:
                    message = message + 's'
                if seconds > 0:
                    message = message + ', '

            if seconds > 0:
                message = message + f'{seconds} seconds'

            message = message + ' remaining!'
        else:
            message = message + 'Time to vote!'

        return message

class IDateTimeProvider:
    def now(self):
        pass

class DateTimeProvider(IDateTimeProvider):
    def now(self):
        return datetime.datetime.now()


class IVoteTownTicker:
    def set_callback(self, cb):
        pass

    def start_ticking(self):
        pass

    def stop_ticking(self):
        pass


class VoteTownTicker(IVoteTownTicker):
    # pylint doesn't understand the @task decorator stuff
    # pylint: disable=no-member

    def __init__(self):
        self.cb = None

    def __del__(self):
        self.tick.cancel()

    def set_callback(self, cb):
        self.cb = cb

    def start_ticking(self):
        if not self.tick.is_running():
            self.tick.start()

    def stop_ticking(self):
        if self.tick.is_running():
            self.tick.stop()

    @tasks.loop(seconds=1)
    async def tick(self):
        await self.cb()


class IMessageBroadcaster:
    async def send_message(self, town_info, message):
        pass

class MessageBroadcaster:
    def __init__(self, bot):
        self.bot = bot

    async def send_message(self, town_info, message):
        if town_info.chat_channel:
            try:
                await town_info.chat_channel.send(message)
            except Exception as ex:
                return f'Unable to send chat message. Do I have permission to send messages to chat channel `{town_info.chat_channel.name}`?\n\n{ex}'


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


class IVoteHandler:
    async def perform_vote(self, town_id):
        pass

class VoteHandler(IVoteHandler):
    def __init__(self, bot, move_cb):
        self.bot = bot
        self.move_cb = move_cb

    async def perform_vote(self, town_id):
        town_info = self.bot.getTownInfoByTownId(town_id)
        await self.move_cb(town_info)


class IVoteTownInfoProvider:
    def get_town_info(self, town_id):
        pass

class VoteTownInfoProvider(IVoteTownInfoProvider):
    def __init__(self, bot):
        self.bot = bot

    def get_town_info(self, town_id):
        town_info:botctypes.TownInfo = self.bot.getTownInfoByTownId(town_id)
        if not town_info:
            return None

        return VoteTownInfo(town_info.chat_channel, town_info.villager_role, town_info.town_square_channel.name)


class VoteTimerImpl:
    def __init__(self, controller, datetime_provider, town_info_provider):
        self.controller = controller
        self.datetime_provider = datetime_provider
        self.town_info_provider = town_info_provider

    async def start_timer(self, town_id, time_in_seconds):
        town_info = self.town_info_provider.get_town_info(town_id)

        if not town_info:
            return 'No town found here. Are you in a town control channel added via the `addTown` or `createTown` commands?'

        if not town_info.chat_channel:
            return 'No chat channel found for this town. Please set the chat channel via the `setChatChannel` command.'

        if not town_info.villager_role:
            return 'No villager role found for this town. Please set up the town properly via the `addTown` command.'

        required_time_str = 'Please choose a time between 15 seconds and 20 minutes.'

        if time_in_seconds < 15:
            return required_time_str

        if time_in_seconds > 1200:
            return required_time_str

        now = self.datetime_provider.now()
        end_time = now+datetime.timedelta(seconds=time_in_seconds)
        return await self.controller.add_town(town_id, end_time)


    async def stop_timer(self, town_id):
        await self.controller.remove_town(town_id)


    def get_seconds_from_string(self, in_str):
        return pytimeparse.parse(in_str)

# Concrete class for use by the Cog
class VoteTimer:
    def __init__(self, bot, move_cb):
        info_provider = VoteTownInfoProvider(bot)
        dt_provider = DateTimeProvider()
        storage = VoteTownStorage(dt_provider)
        ticker = VoteTownTicker()
        broadcaster = MessageBroadcaster(bot)
        vote_handler = VoteHandler(bot, move_cb)
        controller = VoteTimerController(dt_provider, info_provider, storage, ticker, broadcaster, vote_handler)

        self.impl = VoteTimerImpl(controller, dt_provider, info_provider)
        self.bot = bot

    async def start_timer(self, ctx):
        params = shlex.split(ctx.message.content)

        usage = 'Must pass a time string, e.g. "5 minutes 30 seconds" or "5m30s"'

        if len(params) < 2:
            return usage

        time_string = ' '.join(params[1:])
        time_seconds = self.impl.get_seconds_from_string(time_string)
        if not time_seconds:
            return usage

        town_id = botctypes.TownId(ctx.guild.id, ctx.channel.id)
        return await self.impl.start_timer(town_id, time_seconds)

    async def stop_timer(self, ctx):
        town_id = botctypes.TownId(ctx.guild.id, ctx.channel.id)
        return await self.impl.stop_timer(town_id)
