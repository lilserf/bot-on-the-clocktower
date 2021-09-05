import datetime
import unittest
import votetimer

class FakeValidRole:
    def __init__(self):
        self.name = 'role_name'

class TestValidTownInfoProvider(votetimer.IVoteTownInfoProvider):
    def get_town_info(self, town_id):
        valid_channel = {'valid':True}
        return votetimer.VoteTownInfo(valid_channel, FakeValidRole())

class TestDateTimeProvider(votetimer.IDateTimeProvider):
    def __init__(self):
        self.time_now = datetime.datetime.combine(datetime.date(2021, 9, 4), datetime.time(18, 20, 0))

    def now(self):
        return self.time_now

class DoNothingController(votetimer.IVoteTimerController):
    async def add_town(self, town_id, end_time):
        pass
    async def remove_town(self, town_id):
        pass


class TestVoteTimerSync(unittest.TestCase):

    def test_time_parsing(self):
        impl = votetimer.VoteTimerImpl(DoNothingController(), TestDateTimeProvider(), TestValidTownInfoProvider())

        test1 = impl.get_seconds_from_string("5 minutes 30 seconds")
        self.assertEqual(test1, 330, 'Expected "x minutes y seconds" to parse properly')

        test2 = impl.get_seconds_from_string('5:31')
        self.assertEqual(test2, 331, 'Expected "x:y" to parse properly')

        test3 = impl.get_seconds_from_string('5m32s')
        self.assertEqual(test3, 332, 'Expected "xm ys" to parse properly')

        test4 = impl.get_seconds_from_string('foo bar')
        self.assertIsNone(test4, 'Expected bad string to parse to None')


    def test_town_storage(self):

        tdt = TestDateTimeProvider()

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
        valid_channel = {'valid':True}

        class TestNoInfoChannelProvider(votetimer.IVoteTownInfoProvider):
            def get_town_info(self, town_id):
                return None

        impl1 = votetimer.VoteTimerImpl(DoNothingController(), TestDateTimeProvider(), TestNoInfoChannelProvider())
        message = await impl1.start_timer(None, 60)
        self.assertIsNotNone(message)
        self.assertTrue("town" in message and "channel" in message and "found" in message)

        class TestNoChatChannelProvider(votetimer.IVoteTownInfoProvider):
            def get_town_info(self, town_id):
                return votetimer.VoteTownInfo(None, FakeValidRole())
            
        impl2 = votetimer.VoteTimerImpl(DoNothingController(), TestDateTimeProvider(), TestNoChatChannelProvider())
        message = await impl2.start_timer(None, 60)
        self.assertIsNotNone(message)
        self.assertTrue("chat channel" in message and "found" in message)

        class TestNoVillagerRoleProvider(votetimer.IVoteTownInfoProvider):
            def get_town_info(self, town_id):
                return votetimer.VoteTownInfo(valid_channel, None)
            
        impl3 = votetimer.VoteTimerImpl(DoNothingController(), TestDateTimeProvider(), TestNoVillagerRoleProvider())
        message = await impl3.start_timer(None, 60)
        self.assertIsNotNone(message)
        self.assertTrue("role" in message and "villager" in message)

    async def test_start_time_invalid_times(self):
        
        impl = votetimer.VoteTimerImpl(DoNothingController(), TestDateTimeProvider(), TestValidTownInfoProvider())

        message = await impl.start_timer(None, 19)
        self.assertIsNotNone(message)
        self.assertTrue("20 seconds" in message)

        message = await impl.start_timer(None, 1201)
        self.assertIsNotNone(message)
        self.assertTrue("20 minutes" in message)

    async def test_start_time_valid_times(self):
        impl = votetimer.VoteTimerImpl(DoNothingController(), TestDateTimeProvider(), TestValidTownInfoProvider())

        message = await impl.start_timer(None, 20)
        self.assertIsNone(message)
    
        message = await impl.start_timer(None, 300)
        self.assertIsNone(message)
    
        message = await impl.start_timer(None, 1200)
        self.assertIsNone(message)


    async def test_controller(self):

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
                self.has_ret = False

            def add_town(self, town_info, end_time):
                self.add_count = self.add_count+1
                self.add_town_town_info = town_info
                self.add_town_end_time = end_time

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

            async def send_message(self, town_info, message):
                self.send_count = self.send_count+1
                self.last_message = message
                self.last_town_info = town_info

        class TestVoteHandler(votetimer.IVoteHandler):
            def __init__(self):
                self.perform_vote_count = 0

            async def perform_vote(self, town_id):
                self.perform_vote_count = self.perform_vote_count + 1
                self.perform_vote_town_id = town_id

        
        class TestTownInfoProvider(votetimer.IVoteTownInfoProvider):
            def __init__(self):
                self.town_info = votetimer.VoteTownInfo({'valid':True}, FakeValidRole())

            def get_town_info(self, town_id):
                return self.town_info

        ts = TestStorage()
        tt = TestTicker()
        tb = TestBroadcaster()
        td = TestDateTimeProvider()
        tv = TestVoteHandler()
        ti = TestTownInfoProvider()
        
        self.assertIsNone(tt.set_callback_cb)
        self.assertEqual(0, tt.set_count)

        c = votetimer.VoteTimerController(td, ti, ts, tt, tb, tv)
        
        self.assertEqual(1, tt.set_count)
        self.assertIsNotNone(tt.set_callback_cb)
        
        guild_id1 = "guild_id_1"
        channel_id1 = "channel_id_1"
        town1 = votetimer.VoteTownId(guild_id1, channel_id1)

        # Adding a town starts the timer going

        self.assertEqual(0, ts.add_count)
        self.assertEqual(0, tt.start_count)

        town1_end_time = td.time_now+datetime.timedelta(seconds=5)
        await c.add_town(town1, town1_end_time)

        self.assertEqual(1, ts.add_count)
        self.assertEqual(town1, ts.add_town_town_info)
        self.assertEqual(town1_end_time, ts.add_town_end_time)

        self.assertEqual(1, tt.start_count)
        
        self.assertEqual(1, tb.send_count)
        self.assertTrue('5 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)


        # Ticking ticks the storage
        self.assertEqual(0, ts.tick_count)
        ts.tick_ret = []
        ts.has_ret = True

        await tt.set_callback_cb()
        
        self.assertEqual(1, ts.tick_count)


        # End time hit performs vote

        td.time_now = td.time_now+datetime.timedelta(seconds=10)

        self.assertEqual(0, tv.perform_vote_count)
        ts.tick_ret = [town1]
        ts.has_ret = False
        self.assertEqual(0, tt.stop_count)

        await tt.set_callback_cb()

        self.assertEqual(2, ts.tick_count)
        
        self.assertEqual(1, tv.perform_vote_count)
        self.assertEqual(town1, tv.perform_vote_town_id)
        self.assertEqual(1, tt.stop_count)


        # Adds to storage with 10 seconds remaining
        
        town1_end_time = td.time_now+datetime.timedelta(seconds=25)
        await c.add_town(town1, town1_end_time)
        
        self.assertEqual(2, ts.add_count)
        self.assertEqual(town1, ts.add_town_town_info)
        self.assertEqual(town1_end_time-datetime.timedelta(seconds=10), ts.add_town_end_time)

        self.assertEqual(2, tb.send_count)
        self.assertTrue('25 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)
        
        
        # Adds to storage with 300 seconds remaining

        town1_end_time = td.time_now+datetime.timedelta(seconds=350)
        await c.add_town(town1, town1_end_time)
        
        self.assertEqual(3, ts.add_count)
        self.assertEqual(town1, ts.add_town_town_info)
        self.assertEqual(town1_end_time-datetime.timedelta(seconds=300), ts.add_town_end_time)

        self.assertEqual(3, tb.send_count)
        self.assertTrue('5 minutes, 50 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)


        # Fires message when time hits - should be at 288 left here
        
        td.time_now = td.time_now+datetime.timedelta(seconds=62)
        
        await tt.set_callback_cb()

        self.assertEqual(4, tb.send_count)
        self.assertTrue('4 minutes, 50 seconds' in tb.last_message)
        self.assertEqual(ti.town_info, tb.last_town_info)


if __name__ == '__main__':
    unittest.main()