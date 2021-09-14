# pylint: disable=missing-class-docstring, disable=missing-function-docstring, disable=missing-module-docstring, disable=invalid-name, disable=wildcard-import, disable=unused-wildcard-import

import unittest
from datetime import timedelta

from timedcallback import *

class TestTimedCallbackManagerFactory(unittest.TestCase):
    def setUp(self):
        self.mock_loop_factory:MockLoopFactory = MockLoopFactory()
        self.factory:TimedCallbackManagerFactory = TimedCallbackManagerFactory(self.mock_loop_factory)

    def test_construct_no_loops(self):
        self.assertEqual(0, self.mock_loop_factory.create_loop_count)

    def test_request_loop_creates(self):
        manager = self.factory.get_timed_callback_manager(None, timedelta(hours=1))

        self.assertEqual(1, self.mock_loop_factory.create_loop_count)
        self.assertEqual(60*60, self.mock_loop_factory.create_loop_seconds)
        self.assertIsInstance(manager, TimedCallbackManager)

    def test_request_loop_twice_creates_two(self):
        manager1 = self.factory.get_timed_callback_manager(None, timedelta(hours=1))
        manager2 = self.factory.get_timed_callback_manager(None, timedelta(seconds=1))

        self.assertEqual(2, self.mock_loop_factory.create_loop_count)
        self.assertEqual(1, self.mock_loop_factory.create_loop_seconds)
        self.assertIsInstance(manager2, TimedCallbackManager)
        self.assertNotEqual(manager1, manager2)

    def test_request_loop_same_time_creates_one(self):
        manager1 = self.factory.get_timed_callback_manager(None, timedelta(hours=1))
        manager2 = self.factory.get_timed_callback_manager(None, timedelta(hours=1))

        self.assertEqual(manager1, manager2)

class TestTimedCalbackManager(unittest.TestCase):
    # TODO tests for manager
    pass


class MockLoop(ILoop):
    def __init__(self):
        self.start_calls:int = 0
        self.stop_calls:int = 0

    def start(self):
        self.start_calls += 1

    def stop(self):
        self.stop_calls += 1

class MockLoopFactory(ILoopFactory):
    def __init__(self):
        self.create_loop_count:int = 0
        self.create_loop_callback:Callable[[None], None] = 0
        self.create_loop_seconds:int = 0

    def create_loop(self, callback:Callable[[None], None], seconds:int) -> ILoop:
        self.create_loop_count += 1
        self.create_loop_callback = callback
        self.create_loop_seconds = seconds
