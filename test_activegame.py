import activegame
import unittest


class TestActiveGameSync(unittest.TestCase):

    def test_construct_active_game_no_games(self):
        store = activegame.ActiveGameStore(MockActiveGameDb())

        self.assertEqual(0, store.get_active_game_count())

    def test_construct_active_game_with_games(self):
        store1 = activegame.ActiveGameStore(MockActiveGameDb([activegame.ActiveGame()]))
        store2 = activegame.ActiveGameStore(MockActiveGameDb([activegame.ActiveGame(), activegame.ActiveGame()]))

        self.assertEqual(1, store1.get_active_game_count())
        self.assertEqual(2, store2.get_active_game_count())



class MockActiveGameDb(activegame.IActiveGameDb):
    def __init__(self, games:list[activegame.ActiveGame]=[]):
        self.games: list[activegame.ActiveGame] = games

    def retrieve_active_games(self) -> list[activegame.ActiveGame]:
        return self.games


if __name__ == '__main__':
    unittest.main()
