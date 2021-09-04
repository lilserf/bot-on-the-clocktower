import unittest

import votetimer

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

class TestVoteTimerAsync(unittest.IsolatedAsyncioTestCase):
    class TestValidTownInfoProvider(votetimer.VoteTownInfoProviderVirtual):
        def get_town_info(self):
            valid_obj = {'valid':True}
            return votetimer.VoteTownInfo(valid_obj, valid_obj)

    async def test_start_time_invalid_town_info(self):
        impl = votetimer.VoteTimerImpl()
        valid_obj = {'valid':True}

        class TestNoInfoChannelProvider(votetimer.VoteTownInfoProviderVirtual):
            def get_town_info(self):
                return None

        message = await impl.start_timer(TestNoInfoChannelProvider(), 60)
        self.assertIsNotNone(message)
        self.assertTrue("town" in message and "channel" in message and "found" in message)

        class TestNoChatChannelProvider(votetimer.VoteTownInfoProviderVirtual):
            def get_town_info(self):
                return votetimer.VoteTownInfo(None, valid_obj)

        message = await impl.start_timer(TestNoChatChannelProvider(), 60)
        self.assertIsNotNone(message)
        self.assertTrue("chat channel" in message and "found" in message)

        class TestNoVillagerRoleProvider(votetimer.VoteTownInfoProviderVirtual):
            def get_town_info(self):
                return votetimer.VoteTownInfo(valid_obj, None)

        message = await impl.start_timer(TestNoVillagerRoleProvider(), 60)
        self.assertIsNotNone(message)
        self.assertTrue("role" in message and "villager" in message)

    async def test_start_time_invalid_times(self):
        impl = votetimer.VoteTimerImpl()

        message = await impl.start_timer(TestVoteTimerAsync.TestValidTownInfoProvider(), 9)
        self.assertIsNotNone(message)
        self.assertTrue("10 seconds" in message)

        message = await impl.start_timer(TestVoteTimerAsync.TestValidTownInfoProvider(), 1201)
        self.assertIsNotNone(message)
        self.assertTrue("20 minutes" in message)

    async def test_start_time_valid_times(self):
        impl = votetimer.VoteTimerImpl()
    
        message = await impl.start_timer(TestVoteTimerAsync.TestValidTownInfoProvider(), 10)
        self.assertIsNone(message)
    
        message = await impl.start_timer(TestVoteTimerAsync.TestValidTownInfoProvider(), 300)
        self.assertIsNone(message)
    
        message = await impl.start_timer(TestVoteTimerAsync.TestValidTownInfoProvider(), 1200)
        self.assertIsNone(message)

if __name__ == '__main__':
    unittest.main()