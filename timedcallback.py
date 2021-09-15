'''Classes for creating callbacks that happen at particular times'''

import asyncio
from datetime import datetime, timedelta
from typing import Awaitable, Callable

from discord.ext.tasks import Loop

from pythonwrappers import IDateTimeProvider

class ITimedCallbackManager:
    '''Object that checks for timeouts every so often and calls a callback when they hit'''

    def create_or_update_request(self, key:object, calltime:datetime) -> None:
        '''Creates or updates a callback request with a given key to be called at a particular time'''

    def remove_request(self, key:object) -> None:
        '''Removes a request for a callback at a particular time'''

class ITimedCallbackManagerFactory:
    '''Factory for creating ITimedCallbackManagers'''

    def get_timed_callback_manager(self, callback:Callable[[object], Awaitable], check_delta:timedelta) -> ITimedCallbackManager:
        '''Returns an ITimedCallbackManager for calling callbacks and checking every timedelta.'''

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


class TimedCallbackManager(ITimedCallbackManager):
    def __init__(self, datetime_provider:IDateTimeProvider, callback:Callable[[object], Awaitable], on_has_requests:Callable[[], None], on_no_requests:Callable[[], None]):
        self.callback:Callable[[object], Awaitable] = callback
        self.datetime_provider:IDateTimeProvider = datetime_provider
        self.expirations:dict[object, datetime] = {}
        self.on_has_requests:Callable[[], None] = on_has_requests
        self.on_no_requests:Callable[[], None] = on_no_requests

    def create_or_update_request(self, key:object, calltime:datetime) -> None:
        had_requests = self.has_requests()
        self.expirations[key] = calltime

        if not had_requests and self.has_requests():
            self.on_has_requests()

    def remove_request(self, key:object) -> None:
        had_requests = self.has_requests()

        if key in self.expirations:
            self.expirations.pop(key)

        if had_requests and not self.has_requests():
            self.on_no_requests()

    async def tick(self) -> None:
        now = self.datetime_provider.now()

        to_remove:list[object] = []
        for (key, value) in self.expirations.items():
            if value <= now:
                to_remove.append(key)

        for key in to_remove:
            self.expirations.pop(key)

        awaitables = [self.callback(key) for key in to_remove]
        await asyncio.gather(*[task for task in awaitables if task is not None])

        # expected: if this removed all requests, the parent is already ticking and will take care of the on_no_requests() call for us

    def has_requests(self) -> bool:
        return len(self.expirations) > 0

class TimedCallbackManagerFactory(ITimedCallbackManagerFactory):

    class DeltaStorage:
        def __init__(self, loop:ILoop):
            self.loop:ILoop = loop
            self.managers:list[TimedCallbackManager] = []
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


    def __init__(self, datetime_provider:IDateTimeProvider, loop_factory:ILoopFactory):
        self.datetime_provider:IDateTimeProvider = datetime_provider
        self.loop_factory:ILoopFactory = loop_factory
        self.delta_lookup:dict[timedelta, TimedCallbackManagerFactory.DeltaStorage] = {}

    def get_timed_callback_manager(self, callback:Callable[[object], Awaitable], check_delta:timedelta) -> ITimedCallbackManager:
        delta_storage:TimedCallbackManagerFactory.DeltaStorage = self.delta_lookup[check_delta] if check_delta in self.delta_lookup else None
        if delta_storage is None:
            # Was having trouble passing a callback function to the loop factory
            class TickWrapper:
                def __init__(self, time:timedelta, func:Callable[[timedelta], Awaitable[None]]):
                    self.time = time
                    self.func = func
                async def tick(self):
                    await self.func(self.time)
            tick_wrapper = TickWrapper(check_delta, self.on_tick)
            loop = self.loop_factory.create_loop(tick_wrapper.tick, check_delta.total_seconds())
            delta_storage = TimedCallbackManagerFactory.DeltaStorage(loop)
            self.delta_lookup[check_delta] = delta_storage

        manager:TimedCallbackManager = TimedCallbackManager(self.datetime_provider, callback, delta_storage.check_for_loop_start, delta_storage.check_for_loop_stop)
        delta_storage.managers.append(manager)
        return manager

    async def on_tick(self, check_delta:timedelta) -> None:
        delta_storage:TimedCallbackManagerFactory.DeltaStorage = self.delta_lookup[check_delta]
        await delta_storage.tick()

    def on_check_loop_start(self, check_delta:timedelta) -> None:
        self.delta_lookup[check_delta].check_for_loop_start()

    def on_check_loop_stop(self, check_delta:timedelta) -> None:
        self.delta_lookup[check_delta].check_for_loop_stop()



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
