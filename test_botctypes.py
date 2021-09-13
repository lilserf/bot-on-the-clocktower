import botctypes
import unittest

class TestTownId(unittest.TestCase):

    def test_equality(self):

        type1 = botctypes.TownId("guild_id", "channel_id")
        type2 = botctypes.TownId("guild_id", "channel_id")

        self.assertEqual(type1, type2)

    def test_hashing(self):

        type1 = botctypes.TownId("guild_id", "channel_id")
        type2 = botctypes.TownId("guild_id", "channel_id")

        dict = {}

        dict[type1] = "foo"
        dict[type2] = "bar"

        self.assertEqual(1, len(dict.keys()))
        self.assertEqual("bar", dict[type1])

        dict.pop(type2)

        self.assertEqual(0, len(dict.keys()))

if __name__ == '__main__':
    unittest.main()