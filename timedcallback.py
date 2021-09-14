'''Classes for creating callbacks that happen at particular times'''

from datetime import datetime, timedelta
from typing import Callable

from discord.ext.tasks import Loop

class ITimedCallbackManager:
    '''Object that checks for timeouts every so often and calls a callback when they hit'''

    def create_or_update_request(self, key:object, calltime:datetime) -> None:
        '''Creates or updates a callback request with a given key to be called at a particular time'''

    def remove_request(self, key:object) -> None:
        '''Removes a request for a callback at a particular time'''

class ITimedCallbackManagerFactory:
    '''Factory for creating ITimedCallbackManagers'''

    def get_timed_callback_manager(self, callback:Callable[[object], None], check_delta:timedelta) -> ITimedCallbackManager:
        '''Returns an ITimedCallbackManager for calling callbacks and checking every timespan. The callback will take the time.'''

class ILoop():
    '''Interface for a loop, matches API of Loop class in discord.ext'''

    def start(self):
        '''Starts the loop running'''

    def stop(self):
        '''Stops the loop running'''

class ILoopFactory:
    '''Factory for creating Loop objects'''

    def create_loop(self, callback:Callable[[None], None], seconds:int) -> ILoop:
        '''Creates a Loop object with the given times'''


class TimedCallbackManager(ITimedCallbackManager):

    def create_or_update_request(self, key:object, calltime:datetime) -> None:
        pass

    def remove_request(self, key:object) -> None:
        pass

class TimedCallbackManagerFactory(ITimedCallbackManagerFactory):

    def __init__(self, loop_factory:ILoopFactory):
        self.loop_factory:ILoopFactory = loop_factory
        self.manager_lookup:dict[timedelta, TimedCallbackManager] = {}

    def get_timed_callback_manager(self, callback:Callable[[object], None], check_delta:timedelta) -> ITimedCallbackManager:
        self.loop_factory.create_loop(None, check_delta.total_seconds())

        if check_delta not in self.manager_lookup:
            self.manager_lookup[check_delta] = TimedCallbackManager()
        return self.manager_lookup[check_delta]

class LoopFactory(ILoopFactory):
    '''Implementation of ILoopFactory'''

    def create_loop(self, callback:Callable[[None], None], seconds:int) -> None:
        return Loop(
            callback,
            seconds=seconds,
            minutes=0,
            hours=0,
            count=None,
            reconnect=True,
            loop=None)
