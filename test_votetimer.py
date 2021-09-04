import unittest

import votetimer

class TestVoteTimer(unittest.TestCase):

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

if __name__ == '__main__':
    unittest.main()