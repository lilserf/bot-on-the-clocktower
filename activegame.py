'''Module for managing currently-active games the bot tracks'''

from datetime import datetime

from botctypes import TownId
from pythonwrappers import IDateTimeProvider

class SetDifference:
    '''Simple class for figuring what changed when moving from an old set to a new set'''
    def __init__(self, old:set, new:set):
        self.added:set = new-old
        self.removed:set = old-new

class ActiveGame:
    '''Class containing info about an in-progress game'''

    def __init__(self):
        self.town_id: TownId = None
        self.last_activity: datetime = None
        self.storyteller_ids: set[int] = set()
        self.player_ids: set[int] = set()

class IActiveGameStore:
    '''Interface for a class that stores currently-active games'''

    def get_game_count(self) -> int:
        '''Return count of currently active games'''

    def add_or_update_game(self, town_id:TownId, storyteller_ids:set[int], player_ids:set[int]) -> None:
        '''Adds or updates an active game'''

    def get_town_ids_for_member_id(self, member_id:int) -> set[TownId]:
        '''Returns the set of TownIds currently active for a member'''


class IActiveGameDb:
    '''Interface for a class that stores active game information in the database'''

    def retrieve_active_games(self) -> list[ActiveGame]:
        '''Retrieves all active games from the DB'''


class ActiveGameStore(IActiveGameStore):
    '''Storage for holding all the currently-active games'''

    def __init__(self, active_game_db:IActiveGameDb, datetime_provider:IDateTimeProvider):
        self.active_game_db: IActiveGameDb = active_game_db
        self.datetime = datetime_provider

        self.town_ids_from_member_id_map: dict[int, set[TownId]] = {}

        self.current_games:dict[TownId, ActiveGame] = {}
        active_games:list[ActiveGame] = active_game_db.retrieve_active_games()
        game:ActiveGame
        for game in active_games:
            self.on_game_added(game)

    def get_game_count(self) -> int:
        return len(self.current_games)

    def add_or_update_game(self, town_id:TownId, storyteller_ids:set[int], player_ids:set[int]) -> None:
        game:ActiveGame = ActiveGame()
        game.town_id = town_id
        game.storyteller_ids = storyteller_ids
        game.player_ids = player_ids
        game.last_activity = self.datetime.now()

        self.on_game_added(game)
        self.active_game_db.add_or_update_game(game)

    def get_town_ids_for_member_id(self, member_id:int) -> set[TownId]:
        return self.town_ids_from_member_id_map[member_id] if member_id in self.town_ids_from_member_id_map else set()

    def assign_game_to_members(self, game:ActiveGame) -> None:
        '''Assigns a game to the members in that game'''
        unique_ids = set(game.storyteller_ids) | set(game.player_ids)
        for member_id in unique_ids:
            if member_id not in self.town_ids_from_member_id_map:
                self.town_ids_from_member_id_map[member_id] = set()
            self.town_ids_from_member_id_map[member_id].add(game.town_id)

    def on_game_added(self, game:ActiveGame) -> None:
        '''To call whenever a game is added'''

        self.current_games[game.town_id] = game
        self.assign_game_to_members(game)
