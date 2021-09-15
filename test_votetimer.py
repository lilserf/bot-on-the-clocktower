# pylint: disable=missing-class-docstring, disable=missing-function-docstring, disable=missing-module-docstring, disable=invalid-name, disable=wildcard-import, disable=unused-wildcard-import, attribute-defined-outside-init

import unittest

from datetime import datetime, date, time, timedelta
from typing import Awaitable, Callable

from pythonwrappers import IDateTimeProvider

from botctypes import TownId
from votetimer import *
from timedcallback import ITimedCallbackManager, ITimedCallbackManagerFactory


class MockValidRole:
    def __init__(self):
        self.name = 'role_name'

    def mention(self):
        return '@role_name'

class MockValidTownInfoProvider(IVoteTownInfoProvider):
    def mention(self):
        return '@role_name'

class MockDateTimeProvider(IDateTimeProvider):
    def __init__(self):
        self.time_now = datetime.combine(date(2021, 9, 4), time(18, 20, 0))

    def now(self) -> datetime:
        return self.time_now

    def advance(self, delta:timedelta):
        self.time_now = self.time_now + delta

class MockTimedCallbackManager(ITimedCallbackManager):
    def __init__(self, callback:Callable[[object], Awaitable]):
        self.callback:Callable[[object], Awaitable] = callback
        self.key_to_time:[object, datetime] = {}

    def create_or_update_request(self, key:object, calltime:datetime) -> None:
        self.key_to_time[key] = calltime

    def remove_request(self, key:object) -> None:
        self.key_to_time.pop(key)

    async def call_callback(self, key:object) -> None:
        self.remove_request(key)
        await self.callback(key)

class MockTimedCallbackManagerFactory(ITimedCallbackManagerFactory):
    def __init__(self):
        self.managers:list[MockTimedCallbackManager] = []

    def get_timed_callback_manager(self, callback:Callable[[object], Awaitable], check_delta:timedelta) -> MockTimedCallbackManager:
        manager:MockTimedCallbackManager = MockTimedCallbackManager(callback)
        self.managers.append(manager)
        return manager

class MockDoNothingController(IVoteTimerController):
    async def add_town(self, town_id, end_time):
        pass
    async def remove_town(self, town_id):
        pass

class MockBroadcaster(IMessageBroadcaster):
    def __init__(self):
        self.send_count = 0
        self.message_ret = None

    async def send_message(self, town_info, message):
        self.send_count = self.send_count+1
        self.last_message = message
        self.last_town_info = town_info
        return self.message_ret

class MockVoteHandler(IVoteHandler):
    def __init__(self):
        self.perform_vote_count = 0

    async def perform_vote(self, town_id):
        self.perform_vote_count = self.perform_vote_count + 1
        self.perform_vote_town_id = town_id

class MockTownInfoProvider(IVoteTownInfoProvider):
    def __init__(self):
        self.town_info = VoteTownInfo({'valid':True}, MockValidRole(), 'Ye Olde Towne Squarey')

    def get_town_info(self, town_id):
        return self.town_info


class TestVoteTimerSync(unittest.TestCase):

    def test_time_parsing(self):
        test1 = VoteTimerImpl.get_seconds_from_string("5 minutes 30 seconds")
        self.assertEqual(test1, 330, 'Expected "x minutes y seconds" to parse properly')

        test2 = VoteTimerImpl.get_seconds_from_string('5:31')
        self.assertEqual(test2, 331, 'Expected "x:y" to parse properly')

        test3 = VoteTimerImpl.get_seconds_from_string('5m32s')
        self.assertEqual(test3, 332, 'Expected "xm ys" to parse properly')

        test4 = VoteTimerImpl.get_seconds_from_string('foo bar')
        self.assertIsNone(test4, 'Expected bad string to parse to None')


class TestVoteTimerAsync(unittest.IsolatedAsyncioTestCase):


    async def test_start_time_invalid_town_info(self):
        valid_channel = {'valid':True}

        class TestNoInfoChannelProvider(IVoteTownInfoProvider):
            def get_town_info(self, town_id):
                return None

        impl1 = VoteTimerImpl(MockDoNothingController(), MockDateTimeProvider(), TestNoInfoChannelProvider())
        message = await impl1.start_timer(None, 60)
        self.assertIsNotNone(message)
        self.assertTrue("town" in message and "channel" in message and "found" in message)

        class TestNoChatChannelProvider(IVoteTownInfoProvider):
            def get_town_info(self, town_id):
                return VoteTownInfo(None, MockValidRole(), 'Ye Olde Towne Squarey')

        impl2 = VoteTimerImpl(MockDoNothingController(), MockDateTimeProvider(), TestNoChatChannelProvider())
        message = await impl2.start_timer(None, 60)
        self.assertIsNotNone(message)
        self.assertTrue("chat channel" in message and "found" in message)

        class TestNoVillagerRoleProvider(IVoteTownInfoProvider):
            def get_town_info(self, town_id):
                return VoteTownInfo(valid_channel, None, 'Ye Olde Towne Squarey')

        impl3 = VoteTimerImpl(MockDoNothingController(), MockDateTimeProvider(), TestNoVillagerRoleProvider())
        message = await impl3.start_timer(None, 60)
        self.assertIsNotNone(message)
        self.assertTrue("role" in message and "villager" in message)

    async def test_start_time_invalid_times(self):

        impl = VoteTimerImpl(MockDoNothingController(), MockDateTimeProvider(), MockTownInfoProvider())

        message = await impl.start_timer(None, 14)
        self.assertIsNotNone(message)
        self.assertTrue("15 seconds" in message)

        message = await impl.start_timer(None, 1201)
        self.assertIsNotNone(message)
        self.assertTrue("20 minutes" in message)

    async def test_start_time_valid_times(self):
        impl = VoteTimerImpl(MockDoNothingController(), MockDateTimeProvider(), MockTownInfoProvider())

        message = await impl.start_timer(None, 15)
        self.assertIsNone(message)

        message = await impl.start_timer(None, 300)
        self.assertIsNone(message)

        message = await impl.start_timer(None, 1200)
        self.assertIsNone(message)


    async def test_controller(self):

        tcmf = MockTimedCallbackManagerFactory()
        tb = MockBroadcaster()
        td = MockDateTimeProvider()
        tv = MockVoteHandler()
        ti = MockTownInfoProvider()

        self.assertEqual(0, len(tcmf.managers))

        c = VoteTimerController(td, ti, tcmf, tb, tv)

        self.assertEqual(1, len(tcmf.managers))
        tcm = tcmf.managers[0]

        guild_id1 = "guild_id_1"
        channel_id1 = "channel_id_1"
        town1 = TownId(guild_id1, channel_id1)

        # Adding a town starts the timer going

        self.assertEqual(0, len(tcm.key_to_time))

        town1_end_time = td.time_now+timedelta(seconds=5)
        await c.add_town(town1, town1_end_time)

        self.assertEqual(1, len(tcm.key_to_time))
        self.assertTrue(town1 in tcm.key_to_time)
        self.assertEqual(town1_end_time, tcm.key_to_time[town1])

        self.assertEqual(1, tb.send_count)
        self.assertTrue('5 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)


        # End time hit performs vote

        td.advance(timedelta(seconds=10))

        self.assertEqual(0, tv.perform_vote_count)
        self.assertEqual(1, len(tcm.key_to_time))
        self.assertTrue(town1 in tcm.key_to_time)

        await tcm.call_callback(town1)

        self.assertEqual(0, len(tcm.key_to_time))

        self.assertEqual(1, tv.perform_vote_count)
        self.assertEqual(town1, tv.perform_vote_town_id)
        self.assertEqual(2, tb.send_count)
        self.assertTrue('Returning to Ye Olde Towne Squarey to vote!' in tb.last_message)


        # Adds to storage with 15 seconds remaining

        town1_end_time = td.time_now+timedelta(seconds=25)
        await c.add_town(town1, town1_end_time)

        self.assertEqual(1, len(tcm.key_to_time))
        self.assertTrue(town1 in tcm.key_to_time)
        self.assertEqual(town1_end_time-timedelta(seconds=15), tcm.key_to_time[town1])

        self.assertEqual(3, tb.send_count)
        self.assertTrue('25 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)


        # Adds to storage with 300 seconds remaining

        town1_end_time = td.time_now+timedelta(seconds=350)
        await c.add_town(town1, town1_end_time)

        self.assertEqual(1, len(tcm.key_to_time))
        self.assertTrue(town1 in tcm.key_to_time)
        self.assertEqual(town1_end_time-timedelta(seconds=300), tcm.key_to_time[town1])

        self.assertEqual(4, tb.send_count)
        self.assertTrue('5 minutes, 50 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)


        # Fires message when time hits - should be at 288 left here

        td.advance(timedelta(seconds=62))

        await tcm.call_callback(town1)

        self.assertEqual(5, tb.send_count)
        self.assertTrue('4 minutes, 50 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)
        self.assertEqual(1, len(tcmf.managers))


    async def test_broadcaster_returns_value_controller_returns_it(self):

        tcmf = MockTimedCallbackManagerFactory()
        tb = MockBroadcaster()
        td = MockDateTimeProvider()
        tv = MockVoteHandler()
        ti = MockTownInfoProvider()

        c = VoteTimerController(td, ti, tcmf, tb, tv)

        town_id = TownId('guild_id', 'channel_id')
        ret = await c.add_town(town_id, td.time_now+timedelta(seconds=30))

        self.assertIsNone(ret)

        tb.message_ret = 'foo bar'

        ret = await c.add_town(town_id, td.time_now+timedelta(seconds=30))
        self.assertEqual(tb.message_ret, ret)


if __name__ == '__main__':
    unittest.main()
