# pylint: disable=missing-class-docstring, disable=missing-function-docstring, disable=missing-module-docstring, disable=invalid-name, disable=wildcard-import, disable=unused-wildcard-import, disable=too-many-statements

import unittest
from datetime import datetime, timedelta, date, time

from callbackscheduler import *

class TestCallbackScheduler(unittest.IsolatedAsyncioTestCase):
    def setUp(self):
        self.mock_datetime = MockDateTimeProvider()
        self.mock_loop_factory:MockLoopFactory = MockLoopFactory()
        self.factory:CallbackSchedulerFactory = CallbackSchedulerFactory(self.mock_datetime, self.mock_loop_factory)

    def test_construct_no_loops(self):
        self.assertEqual(0, self.mock_loop_factory.create_loop_count)

    def test_request_loop_creates(self):
        manager = self.factory.get_scheduler(None, timedelta(hours=1))

        self.assertEqual(1, self.mock_loop_factory.create_loop_count)
        self.assertEqual(60*60, self.mock_loop_factory.create_loop_seconds)
        self.assertIsInstance(manager, CallbackScheduler)

    def test_request_loop_twice_creates_two_managers(self):
        manager1 = self.factory.get_scheduler(None, timedelta(hours=1))
        manager2 = self.factory.get_scheduler(None, timedelta(seconds=1))

        self.assertEqual(2, self.mock_loop_factory.create_loop_count)
        self.assertEqual(1, self.mock_loop_factory.create_loop_seconds)
        self.assertIsInstance(manager2, CallbackScheduler)
        self.assertNotEqual(manager1, manager2)

    def test_request_loop_same_time_creates_one_loop(self):
        manager1 = self.factory.get_scheduler(None, timedelta(hours=1))
        manager2 = self.factory.get_scheduler(None, timedelta(hours=1))

        self.assertNotEqual(manager1, manager2)
        self.assertEqual(1, len(self.mock_loop_factory.loops))

    async def test_add_request_callback_called_after_time(self):

        class CallTester:
            def __init__(self):
                self.times_called_1 = 0
                self.times_called_2 = 0
                self.times_called_3 = 0
                self.times_called_wrong_key = 0
                self.key_1 = 'key1'
                self.key_2 = 'key2'
                self.key_3 = 'key3'

            def on_call_1(self, call_key):
                if call_key == self.key_1:
                    self.times_called_1 += 1
                else:
                    self.times_called_wrong_key += 1

            def on_call_2(self, call_key):
                if call_key == self.key_2:
                    self.times_called_2 += 1
                elif call_key == self.key_3:
                    self.times_called_3 += 1
                else:
                    self.times_called_wrong_key += 1

        calltester:CallTester = CallTester()

        self.assertEqual(0, len(self.mock_loop_factory.loops))
        manager_1 = self.factory.get_scheduler(calltester.on_call_1, timedelta(hours=1))
        self.assertEqual(1, len(self.mock_loop_factory.loops))
        manager_2 = self.factory.get_scheduler(calltester.on_call_2, timedelta(hours=1))
        self.assertEqual(1, len(self.mock_loop_factory.loops))

        self.assertEqual(0, calltester.times_called_1)
        self.assertEqual(0, calltester.times_called_2)
        self.assertEqual(0, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        manager_1.schedule_callback(calltester.key_1, self.mock_datetime.now() + timedelta(hours=2))
        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        manager_2.schedule_callback(calltester.key_2, self.mock_datetime.now() + timedelta(hours=3))
        manager_2.schedule_callback(calltester.key_3, self.mock_datetime.now() + timedelta(hours=4))
        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        self.assertEqual(0, calltester.times_called_1)
        self.assertEqual(0, calltester.times_called_2)
        self.assertEqual(0, calltester.times_called_3)

        # Advance 1 hour, nothing should trigger
        self.mock_datetime.advance(timedelta(seconds=1) + timedelta(hours=1))
        await self.mock_loop_factory.create_loop_callback()

        self.assertEqual(0, calltester.times_called_1)
        self.assertEqual(0, calltester.times_called_2)
        self.assertEqual(0, calltester.times_called_3)

        # Advance another hour, just call 1 should trigger
        self.mock_datetime.advance(timedelta(hours=1))
        await self.mock_loop_factory.create_loop_callback()

        self.assertEqual(1, calltester.times_called_1)
        self.assertEqual(0, calltester.times_called_2)
        self.assertEqual(0, calltester.times_called_3)

        # Advance another hour, just call 2 should trigger
        self.mock_datetime.advance(timedelta(hours=1))
        await self.mock_loop_factory.create_loop_callback()

        self.assertEqual(1, calltester.times_called_1)
        self.assertEqual(1, calltester.times_called_2)
        self.assertEqual(0, calltester.times_called_3)

        # Advance another hour, just call for key 3 should trigger
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        self.mock_datetime.advance(timedelta(hours=1))
        await self.mock_loop_factory.create_loop_callback()

        self.assertEqual(1, calltester.times_called_1)
        self.assertEqual(1, calltester.times_called_2)
        self.assertEqual(1, calltester.times_called_3)
        self.assertEqual(0, calltester.times_called_wrong_key)

        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(1, self.mock_loop_factory.loops[0].stop_calls)
        self.assertEqual(1, len(self.mock_loop_factory.loops))

    def test_request_loop_cancel_request_loop_stopped(self):
        manager = self.factory.get_scheduler(None, timedelta(hours=1))
        self.assertEqual(1, len(self.mock_loop_factory.loops))
        self.assertEqual(0, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)

        manager.schedule_callback('key', self.mock_datetime.now() + timedelta(hours=2))
        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)

        manager.cancel_callback('key')
        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(1, self.mock_loop_factory.loops[0].stop_calls)

    async def test_update_request_moves_forward(self):
        class CallTester:
            def __init__(self):
                self.num_calls = 0
                self.key = 'key'

            def on_call(self, call_key):
                if call_key == self.key:
                    self.num_calls += 1

        calltester = CallTester()
        manager = self.factory.get_scheduler(calltester.on_call, timedelta(seconds=5))
        self.assertEqual(0, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        self.assertEqual(0, calltester.num_calls)
        manager.schedule_callback(calltester.key, self.mock_datetime.now() + timedelta(seconds=2))
        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        self.assertEqual(0, calltester.num_calls)
        manager.schedule_callback(calltester.key, self.mock_datetime.now() + timedelta(seconds=7))
        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        self.assertEqual(0, calltester.num_calls)

        self.mock_datetime.advance(timedelta(seconds=5))
        await self.mock_loop_factory.create_loop_callback()

        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(0, self.mock_loop_factory.loops[0].stop_calls)
        self.assertEqual(0, calltester.num_calls)

        self.mock_datetime.advance(timedelta(seconds=5))
        await self.mock_loop_factory.create_loop_callback()

        self.assertEqual(1, self.mock_loop_factory.loops[0].start_calls)
        self.assertEqual(1, self.mock_loop_factory.loops[0].stop_calls)
        self.assertEqual(1, calltester.num_calls)



class MockLoop(ILoop):
    def __init__(self):
        self.start_calls:int = 0
        self.stop_calls:int = 0

    def start(self) -> None:
        self.start_calls += 1

    def stop(self) -> None:
        self.stop_calls += 1

    def is_running(self) -> bool:
        return self.start_calls > self.stop_calls

class MockLoopFactory(ILoopFactory):
    def __init__(self):
        self.create_loop_count:int = 0
        self.create_loop_callback:Callable[[None], Awaitable] = None
        self.create_loop_seconds:int = 0
        self.loops:list[MockLoop] = []

    def create_loop(self, callback:Callable[[None], Awaitable], seconds:int) -> ILoop:
        self.create_loop_count += 1
        self.create_loop_callback = callback
        self.create_loop_seconds = seconds
        loop = MockLoop()
        self.loops.append(loop)
        return loop


class MockDateTimeProvider(IDateTimeProvider):
    def __init__(self):
        self.time_now = datetime.combine(date(2021, 9, 4), time(18, 20, 0))

    def now(self) -> datetime:
        return self.time_now

    def advance(self, delta:timedelta):
        self.time_now = self.time_now + delta
