﻿# pylint: disable=missing-class-docstring, disable=missing-function-docstring, disable=missing-module-docstring, disable=invalid-name, disable=wildcard-import, disable=unused-wildcard-import

from datetime import datetime, date, time
import unittest

from activegame import *
from pythonwrappers import *

class TestActiveGameSync(unittest.TestCase):

    def test_construct_active_game_no_games(self):
        store = ActiveGameStore(MockActiveGameDb([]), MockDateTimeProvider())

        self.assertEqual(0, store.get_game_count())

    def test_construct_active_game_with_games(self):
        g1 = ActiveGame()
        g1.town_id = TownId(1, 1)
        g2 = ActiveGame()
        g2.town_id = TownId(2, 2)

        store1 = ActiveGameStore(MockActiveGameDb([g1]), MockDateTimeProvider())
        store2 = ActiveGameStore(MockActiveGameDb([g1, g2]), MockDateTimeProvider())

        self.assertEqual(1, store1.get_game_count())
        self.assertEqual(2, store2.get_game_count())

    def test_add_new_game_updates_stat_and_db(self):
        db_mock:MockActiveGameDb = MockActiveGameDb([])
        store:ActiveGameStore = ActiveGameStore(db_mock, MockDateTimeProvider())

        town_id:TownId = TownId(6, 8)
        storyteller_ids:list[int] = [1]
        player_ids:list[int] = [1, 2, 3]

        store.add_or_update_game(town_id, storyteller_ids, player_ids)

        self.assertEqual(1, store.get_game_count())
        self.assertEqual(1, db_mock.add_or_update_game_calls)
        self.assertEqual(town_id, db_mock.add_or_update_game_town_id)
        self.assertEqual(town_id, db_mock.add_or_update_game_game.town_id)
        self.assertSetEqual(set(storyteller_ids), db_mock.add_or_update_game_game.storyteller_ids)
        self.assertSetEqual(set(player_ids), db_mock.add_or_update_game_game.player_ids)

        self.assertSetEqual(set(), store.get_town_ids_for_member_id(0))
        self.assertSetEqual(set([town_id]), store.get_town_ids_for_member_id(1))
        self.assertSetEqual(set([town_id]), store.get_town_ids_for_member_id(2))
        self.assertSetEqual(set([town_id]), store.get_town_ids_for_member_id(3))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(4))

    def test_construct_active_game_with_games_lookup_successful(self):

        g1 = ActiveGame()
        g1.player_ids = set([1, 2])
        g1.storyteller_ids = set([3])
        g1.town_id = TownId(10, 11)

        g2 = ActiveGame()
        g2.player_ids = set([2, 4])
        g2.storyteller_ids = set([4])
        g2.town_id = TownId(14, 15)

        store = ActiveGameStore(MockActiveGameDb([g1, g2]), MockDateTimeProvider())

        self.assertEqual(2, store.get_game_count())
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(0))
        self.assertSetEqual(set([g1.town_id]), store.get_town_ids_for_member_id(1))
        self.assertSetEqual(set([g1.town_id, g2.town_id]), store.get_town_ids_for_member_id(2))
        self.assertSetEqual(set([g1.town_id]), store.get_town_ids_for_member_id(3))
        self.assertSetEqual(set([g2.town_id]), store.get_town_ids_for_member_id(4))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(5))

    def test_set_difference_no_change(self):
        set_diff = SetDifference(set(), set())
        self.assertSetEqual(set(), set_diff.added)
        self.assertSetEqual(set(), set_diff.removed)

    def test_set_difference_only_adds(self):
        set_diff = SetDifference(set([1, 2, 3]), set())
        self.assertSetEqual(set([1, 2, 3]), set_diff.added)
        self.assertSetEqual(set(), set_diff.removed)

    def test_set_difference_only_removes(self):
        set_diff = SetDifference(set(), set([1, 2, 3]))
        self.assertSetEqual(set(), set_diff.added)
        self.assertSetEqual(set([1, 2, 3]), set_diff.removed)

    def test_set_difference_adds_and_removes(self):
        set_diff = SetDifference(set([2, 3]), set([1, 2]))
        self.assertSetEqual(set([3]), set_diff.added)
        self.assertSetEqual(set([1]), set_diff.removed)

    def test_existing_game_new_game_replaces(self):
        town_id = TownId(10, 11)

        g1 = ActiveGame()
        g1.player_ids = set([1, 2])
        g1.storyteller_ids = set([1])
        g1.town_id = town_id

        store = ActiveGameStore(MockActiveGameDb([g1]), MockDateTimeProvider())

        self.assertEqual(1, store.get_game_count())
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(0))
        self.assertSetEqual(set([town_id]), store.get_town_ids_for_member_id(1))
        self.assertSetEqual(set([town_id]), store.get_town_ids_for_member_id(2))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(3))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(4))

        store.add_or_update_game(TownId(10, 11), set([2, 3]), set([2]))

        self.assertEqual(1, store.get_game_count())
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(0))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(1))
        self.assertSetEqual(set([town_id]), store.get_town_ids_for_member_id(2))
        self.assertSetEqual(set([town_id]), store.get_town_ids_for_member_id(3))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(4))

    def test_existing_game_remove_removes(self):
        town_id_1 = TownId(1, 1)
        town_id_2 = TownId(2, 2)

        g1 = ActiveGame()
        g1.player_ids = set([1, 2])
        g1.storyteller_ids = set([3])
        g1.town_id = town_id_1

        g2 = ActiveGame()
        g2.player_ids = set([1, 4])
        g2.storyteller_ids = set([4])
        g2.town_id = town_id_2
        
        mock_db:MockActiveGameDb = MockActiveGameDb([g1, g2])
        store:ActiveGameStore = ActiveGameStore(mock_db, MockDateTimeProvider())

        self.assertEqual(2, store.get_game_count())
        self.assertSetEqual(set([g1.town_id, g2.town_id]), store.get_town_ids_for_member_id(1))
        self.assertSetEqual(set([g1.town_id]), store.get_town_ids_for_member_id(2))
        self.assertSetEqual(set([g1.town_id]), store.get_town_ids_for_member_id(3))
        
        self.assertEqual(0, mock_db.remove_game_calls)

        store.remove_game(g1.town_id)

        self.assertEqual(1, store.get_game_count())
        self.assertSetEqual(set([g2.town_id]), store.get_town_ids_for_member_id(1))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(2))
        self.assertSetEqual(set(), store.get_town_ids_for_member_id(3))

        self.assertEqual(1, mock_db.remove_game_calls)
        self.assertEqual(g1.town_id, mock_db.remove_game_town_id)



class MockActiveGameDb(IActiveGameDb):

    def __init__(self, games:list[ActiveGame]):
        self.games: list[ActiveGame] = games

        self.add_or_update_game_calls:int = 0
        self.add_or_update_game_town_id:TownId = None
        self.add_or_update_game_game:ActiveGame = None

        self.remove_game_calls:int = 0
        self.remove_game_town_id:TownId = None

    def retrieve_active_games(self) -> list[ActiveGame]:
        return self.games

    def add_or_update_game(self, town_id:TownId, game:ActiveGame) -> None:
        self.add_or_update_game_calls += 1
        self.add_or_update_game_town_id = town_id
        self.add_or_update_game_game = game

    def remove_game(self, town_id:TownId) -> None:
        self.remove_game_calls += 1
        self.remove_game_town_id = town_id


class MockDateTimeProvider(IDateTimeProvider):
    def __init__(self):
        self.time_now = datetime.combine(date(2021, 9, 4), time(18, 20, 0))

    def now(self) -> datetime:
        return self.time_now


if __name__ == '__main__':
    unittest.main()