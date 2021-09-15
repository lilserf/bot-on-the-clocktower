'''Classes for creating callbacks that happen at particular times'''

import asyncio
from datetime import datetime, timedelta
from typing import Awaitable, Callable

from discord.ext.tasks import Loop

from pythonwrappers import IDateTimeProvider

class ICallbackScheduler:
    '''Object to schedule callbacks at specific times'''

    def schedule_callback(self, key:object, calltime:datetime) -> None:
        '''Creates or updates a callback request with a given key to be called at a particular time'''

    def cancel_callback(self, key:object) -> None:
        '''Removes a request for a callback at a particular time'''

class ICallbackSchedulerFactory:
    '''Factory for creating ICallbackSchedulers'''

    def get_scheduler(self, callback:Callable[[object], Awaitable], frequency:timedelta) -> ICallbackScheduler:
        '''Returns an ICallbackScheduler that checks for timeouts at a given frequency.'''

class ILoop():
    '''Interface for a loop'''

    def is_running(self) -> bool:
        '''Checks whether the loop is currently running'''

    def start(self) -> None:
        '''Starts the loop running'''

    def stop(self) -> None:
        '''Stops the loop running'''

class ILoopFactory:
    '''Factory for creating Loop objects'''

    def create_loop(self, callback:Callable[[None], None], seconds:int) -> ILoop:
        '''Creates a Loop object with the given times'''


class CallbackScheduler(ICallbackScheduler):
    def __init__(self, datetime_provider:IDateTimeProvider, callback:Callable[[object], Awaitable], on_has_requests:Callable[[], None], on_no_requests:Callable[[], None]):
        self.callback:Callable[[object], Awaitable] = callback
        self.datetime_provider:IDateTimeProvider = datetime_provider
        self.scheduled:dict[object, datetime] = {}
        self.on_has_requests:Callable[[], None] = on_has_requests
        self.on_no_requests:Callable[[], None] = on_no_requests

    def schedule_callback(self, key:object, calltime:datetime) -> None:
        had_requests = self.has_requests()
        self.scheduled[key] = calltime

        if not had_requests and self.has_requests():
            self.on_has_requests()

    def cancel_callback(self, key:object) -> None:
        had_requests = self.has_requests()

        if key in self.scheduled:
            self.scheduled.pop(key)

        if had_requests and not self.has_requests():
            self.on_no_requests()

    async def tick(self) -> None:
        now = self.datetime_provider.now()

        to_remove:list[object] = []
        for (key, value) in self.scheduled.items():
            if value <= now:
                to_remove.append(key)

        for key in to_remove:
            self.scheduled.pop(key)

        awaitables = [self.callback(key) for key in to_remove]
        await asyncio.gather(*[task for task in awaitables if task is not None])

        # expected: if this removed all requests, the parent is already ticking and will take care of the on_no_requests() call for us

    def has_requests(self) -> bool:
        return len(self.scheduled) > 0

class CallbackSchedulerLoopManager:
    def __init__(self, loop:ILoop):
        self.loop:ILoop = loop
        self.managers:list[CallbackScheduler] = []
        self.is_ticking:bool = False

    async def tick(self) -> None:
        if not self.is_ticking:
            self.is_ticking = True
            awaitables = [manager.tick() for manager in self.managers if manager.has_requests()]
            await asyncio.gather(*[task for task in awaitables if task is not None])
            self.is_ticking = False

            self.check_for_loop_stop()

    def check_for_loop_start(self) -> None:
        if not self.is_ticking and not self.loop.is_running():
            should_start:bool = False
            for manager in self.managers:
                if manager.has_requests():
                    should_start = True
                    break
            if should_start:
                self.loop.start()

    def check_for_loop_stop(self) -> None:
        if not self.is_ticking and self.loop.is_running():
            should_stop:bool = True
            for manager in self.managers:
                if manager.has_requests():
                    should_stop = False
                    break
            if should_stop:
                self.loop.stop()

class CallbackSchedulerFactory(ICallbackSchedulerFactory):
    def __init__(self, datetime_provider:IDateTimeProvider, loop_factory:ILoopFactory):
        self.datetime_provider:IDateTimeProvider = datetime_provider
        self.loop_factory:ILoopFactory = loop_factory
        self.loop_manager_lookup:dict[timedelta, CallbackSchedulerLoopManager] = {}

    def get_scheduler(self, callback:Callable[[object], Awaitable], frequency:timedelta) -> ICallbackScheduler:
        loop_manager:CallbackSchedulerLoopManager = self.loop_manager_lookup[frequency] if frequency in self.loop_manager_lookup else None
        if loop_manager is None:
            # Was having trouble passing a callback function to the loop factory
            class TickWrapper:
                def __init__(self, time:timedelta, func:Callable[[timedelta], Awaitable[None]]):
                    self.time = time
                    self.func = func
                async def tick(self):
                    await self.func(self.time)
            tick_wrapper = TickWrapper(frequency, self.on_tick)
            loop = self.loop_factory.create_loop(tick_wrapper.tick, frequency.total_seconds())
            loop_manager = CallbackSchedulerLoopManager(loop)
            self.loop_manager_lookup[frequency] = loop_manager

        manager:CallbackScheduler = CallbackScheduler(self.datetime_provider, callback, loop_manager.check_for_loop_start, loop_manager.check_for_loop_stop)
        loop_manager.managers.append(manager)
        return manager

    async def on_tick(self, frequency:timedelta) -> None:
        loop_manager:CallbackSchedulerLoopManager = self.loop_manager_lookup[frequency]
        await loop_manager.tick()

    def on_check_loop_start(self, frequency:timedelta) -> None:
        self.loop_manager_lookup[frequency].check_for_loop_start()

    def on_check_loop_stop(self, frequency:timedelta) -> None:
        self.loop_manager_lookup[frequency].check_for_loop_stop()



class DiscordExtLoopFactory(ILoopFactory):
    '''Implementation of ILoopFactory using the discord.ext Loop class'''

    def create_loop(self, callback:Callable[[None], Awaitable], seconds:int) -> None:
        return DiscordExtLoopWrapper(Loop(
            callback,
            seconds=seconds,
            minutes=0,
            hours=0,
            count=None,
            reconnect=True,
            loop=None))

class DiscordExtLoopWrapper(ILoop):
    '''Wrapper around the discord.ext Loop class'''

    def __init__(self, loop:Loop):
        self.loop:Loop = loop

    def is_running(self) -> bool:
        return self.loop.is_running()

    def start(self) -> None:
        self.loop.start()

    def stop(self) -> None:
        self.loop.stop()
