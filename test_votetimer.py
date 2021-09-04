import datetime
import unittest
import votetimer

class TestValidTownInfoProvider(votetimer.IVoteTownInfoProvider):
    def get_town_info(self):
        valid_obj = {'valid':True}
        return votetimer.VoteTownInfo(valid_obj, valid_obj)


class TestVoteTimerSync(unittest.TestCase):

    def test_time_parsing(self):
        impl = votetimer.VoteTimerImpl()

        test1 = impl.get_seconds_from_string("5 minutes 30 seconds")
        self.assertEqual(test1, 330, 'Expected "x minutes y seconds" to parse properly')

        test2 = impl.get_seconds_from_string('5:31')
        self.assertEqual(test2, 331, 'Expected "x:y" to parse properly')

        test3 = impl.get_seconds_from_string('5m32s')
        self.assertEqual(test3, 332, 'Expected "xm ys" to parse properly')

        test4 = impl.get_seconds_from_string('foo bar')
        self.assertIsNone(test4, 'Expected bad string to parse to None')


    def test_town_storage(self):

        class TestDateTimeProvider(votetimer.IDateTimeProvider):
            def now(self):
                return self.time_now

        tdt = TestDateTimeProvider()

        tdt.time_now = datetime.datetime.combine(datetime.date(2021, 9, 4), datetime.time(18, 20, 0))

        ts = votetimer.VoteTownStorage(tdt)

        self.assertFalse(ts.has_towns_ticking(), 'should start with no ticking towns')

        t1 = TestValidTownInfoProvider()
        t2 = TestValidTownInfoProvider()

        ts.add_town(t1, tdt.time_now+datetime.timedelta(seconds=5))
        
        self.assertTrue(ts.has_towns_ticking(), 'should now have ticking town')

        ret = ts.tick_and_return_finished_towns()
        self.assertEqual(0, len(ret), 'should not return towns still ticking')
        self.assertTrue(ts.has_towns_ticking(), 'towns should still be ticking')

        tdt.time_now = tdt.time_now+datetime.timedelta(seconds=5)

        ret = ts.tick_and_return_finished_towns()
        self.assertEqual(1, len(ret), 'should return single ticking town')
        self.assertEqual(t1, ret[0], 'should return town done ticking')
        self.assertFalse(ts.has_towns_ticking(), 'should be no more towns ticking')

        ts.add_town(t1, tdt.time_now+datetime.timedelta(seconds=5))

        ret = ts.tick_and_return_finished_towns()
        self.assertEqual(0, len(ret), 'should not return towns still ticking')
        self.assertTrue(ts.has_towns_ticking(), 'towns should still be ticking')
            
        ts.add_town(t1, tdt.time_now+datetime.timedelta(seconds=10))
        ts.add_town(t2, tdt.time_now+datetime.timedelta(seconds=10))

        tdt.time_now = tdt.time_now+datetime.timedelta(seconds=9)

        ret = ts.tick_and_return_finished_towns()
        self.assertEqual(0, len(ret), 'should not return towns still ticking')
        self.assertTrue(ts.has_towns_ticking(), 'towns should still be ticking')
        
        tdt.time_now = tdt.time_now+datetime.timedelta(seconds=1)

        ret = ts.tick_and_return_finished_towns()
        self.assertEqual(2, len(ret), 'should return both ticking towns')
        self.assertTrue(t1 in ret, 'should return t1 done ticking')
        self.assertTrue(t2 in ret, 'should return t2 done ticking')
        self.assertFalse(ts.has_towns_ticking(), 'should be no more towns ticking')
        
        ts.add_town(t1, tdt.time_now+datetime.timedelta(seconds=1))
        ts.add_town(t2, tdt.time_now+datetime.timedelta(seconds=1))
        self.assertTrue(ts.has_towns_ticking(), 'should be towns ticking')
        ts.remove_town(t2)
        self.assertTrue(ts.has_towns_ticking(), 'should be towns ticking')
        ts.remove_town(t2)
        self.assertTrue(ts.has_towns_ticking(), 'should be towns ticking')
        ts.remove_town(t1)
        self.assertFalse(ts.has_towns_ticking(), 'should be no towns ticking')
        
        ret = ts.tick_and_return_finished_towns()
        self.assertEqual(0, len(ret), 'should not return towns still ticking')
        self.assertFalse(ts.has_towns_ticking(), 'no towns should be ticking')


class TestVoteTimerAsync(unittest.IsolatedAsyncioTestCase):

    async def test_start_time_invalid_town_info(self):
        impl = votetimer.VoteTimerImpl()
        valid_obj = {'valid':True}

        class TestNoInfoChannelProvider(votetimer.IVoteTownInfoProvider):
            def get_town_info(self):
                return None

        message = await impl.start_timer(TestNoInfoChannelProvider(), 60)
        self.assertIsNotNone(message)
        self.assertTrue("town" in message and "channel" in message and "found" in message)

        class TestNoChatChannelProvider(votetimer.IVoteTownInfoProvider):
            def get_town_info(self):
                return votetimer.VoteTownInfo(None, valid_obj)

        message = await impl.start_timer(TestNoChatChannelProvider(), 60)
        self.assertIsNotNone(message)
        self.assertTrue("chat channel" in message and "found" in message)

        class TestNoVillagerRoleProvider(votetimer.IVoteTownInfoProvider):
            def get_town_info(self):
                return votetimer.VoteTownInfo(valid_obj, None)

        message = await impl.start_timer(TestNoVillagerRoleProvider(), 60)
        self.assertIsNotNone(message)
        self.assertTrue("role" in message and "villager" in message)

    async def test_start_time_invalid_times(self):
        impl = votetimer.VoteTimerImpl()

        message = await impl.start_timer(TestValidTownInfoProvider(), 9)
        self.assertIsNotNone(message)
        self.assertTrue("10 seconds" in message)

        message = await impl.start_timer(TestValidTownInfoProvider(), 1201)
        self.assertIsNotNone(message)
        self.assertTrue("20 minutes" in message)

    async def test_start_time_valid_times(self):
        impl = votetimer.VoteTimerImpl()
    
        message = await impl.start_timer(TestValidTownInfoProvider(), 10)
        self.assertIsNone(message)
    
        message = await impl.start_timer(TestValidTownInfoProvider(), 300)
        self.assertIsNone(message)
    
        message = await impl.start_timer(TestValidTownInfoProvider(), 1200)
        self.assertIsNone(message)


    async def test_countdowns(self):

        class TestTicker(votetimer.IVoteTownTicker):
            def __init__(self):
                self.set_count = 0
                self.start_count = 0
                self.stop_count = 0

                self.set_callback_cb = None

            def set_callback(self, cb):
                self.set_count = self.set_count+1
                self.set_callback_cb = cb

            def start_ticking(self):
                self.start_count = self.start_count+1

            def stop_ticking(self):
                self.stop_count = self.stop_count+1

        class TestStorage(votetimer.IVoteTownStorage):
            def __init__(self):
                self.add_count = 0
                self.remove_count = 0
                self.tick_count = 0
                self.has_count = 0

            def add_town(self, town_info, ticks):
                self.add_count = self.add_count+1
                self.add_town_town_info = town_info
                self.add_town_ticks = ticks

            def remove_town(self, town_info):
                self.remove_count = self.remove_count+1
                self.remove_town_town_info = town_info

            def tick_and_return_finished_towns(self):
                self.tick_count = self.tick_count+1
                return self.tick_ret

            def has_towns_ticking(self):
                self.has_count = self.has_count+1
                return self.has_ret


        class TestBroadcaster(votetimer.IMessageBroadcaster):
            def __init__(self):
                self.send_count = 0

            def send_message(self, message):
                self.send_count = self.send_count+1
                self.last_message = message

                
        ts = TestStorage()
        tt = TestTicker()
        tb = TestBroadcaster()
        
        self.assertIsNone(tt.set_callback_cb)
        self.assertEqual(0, tt.set_count)

        cd = votetimer.VoteTimerCountdown(ts, tt, tb)
        
        self.assertIsNotNone(tt.set_callback_cb)

if __name__ == '__main__':
    unittest.main()