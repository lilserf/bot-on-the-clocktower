from botctypes import TownId
from datetime import datetime
from pythonwrappers import IDateTimeProvider

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
    def __init__(self, active_game_db:IActiveGameDb, datetime_provider:IDateTimeProvider):
        self.db: IActiveGameDb = active_game_db
        self.datetime = datetime_provider

        self.town_ids_from_member_id_map: dict[int, set[TownId]] = {}
        self.current_games:list[ActiveGame] = active_game_db.retrieve_active_games()

        for g in self.current_games:
            self.add_game_for_members(g)
    
    def get_game_count(self) -> int:
        return len(self.current_games)

    def add_or_update_game(self, town_id:TownId, storyteller_ids:set[int], player_ids:set[int]) -> None:
        game:ActiveGame = ActiveGame()
        game.town_id = town_id
        game.storyteller_ids = storyteller_ids
        game.player_ids = player_ids
        game.last_activity = self.datetime.now()

        self.current_games.append(game)
        self.add_game_for_members(game)

        self.db.add_or_update_game(game)

    def get_town_ids_for_member_id(self, member_id:int) -> set[TownId]:
        return member_id in self.town_ids_from_member_id_map and self.town_ids_from_member_id_map[member_id] or set()

    def add_game_for_members(self, game:ActiveGame) -> None:
        unique_ids = set(game.storyteller_ids) | set(game.player_ids)
        for id in unique_ids:
            if id not in self.town_ids_from_member_id_map:
                self.town_ids_from_member_id_map[id] = set()
            self.town_ids_from_member_id_map[id].add(game.town_id)
