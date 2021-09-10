from botctypes import TownId
from datetime import datetime

class ActiveGame:
    '''Class containing info about an in-progress game'''
    def __init__(self):
        self.town_id: TownId = None
        self.last_activity: None
        self.storyteller_ids: list[int] = []
        self.player_ids: list[int] = []

class IActiveGameStore:
    '''Interface for a class that stores currently-active games'''

    def get_active_game_count(self) -> int:
        '''Return count of currently active games'''

class IActiveGameDb:
    '''Interface for a class that stores active game information in the database'''

    def retrieve_active_games(self) -> list[ActiveGame]:
        '''Retrieves all active games from the DB'''

class ActiveGameStore(IActiveGameStore):
    def __init__(self, active_game_db:IActiveGameDb):
        self.db: IActiveGameDb = active_game_db
        self.current_games: list[ActiveGame] = active_game_db.retrieve_active_games()
    
    def get_active_game_count(self) -> int:
        return len(self.current_games)
