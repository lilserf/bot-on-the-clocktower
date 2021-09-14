'''Module for managing currently-active games the bot tracks'''

from datetime import datetime

from pymongo.collection import Collection
from pymongo.database import Database

import botcmongo
from botctypes import TownId
from pythonwrappers import IDateTimeProvider

class SetDifference:
    '''Simple class for figuring what changed when moving from an old set to a new set'''
    def __init__(self, new:set, old:set=None):
        old = set() if old is None else old
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

    def add_or_update_game(self, game:ActiveGame) -> None:
        '''Stores a game in the database'''

    def remove_game(self, game:ActiveGame) -> None:
        '''Removes a game from the database'''


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
            self.on_game_added(game, SetDifference(game.storyteller_ids | game.player_ids))

    def get_game_count(self) -> int:
        return len(self.current_games)

    def add_or_update_game(self, town_id:TownId, storyteller_ids:set[int], player_ids:set[int]) -> None:
        old_game = self.current_games[town_id] if town_id in self.current_games else ActiveGame()
        all_old_members = old_game.storyteller_ids | old_game.player_ids

        new_game:ActiveGame = ActiveGame()
        new_game.town_id = town_id
        new_game.storyteller_ids = set(storyteller_ids)
        new_game.player_ids = set(player_ids)
        new_game.last_activity = self.datetime.now()

        all_new_members = set(storyteller_ids) | set(player_ids)
        member_diff = SetDifference(all_new_members, all_old_members)

        self.on_game_added(new_game, member_diff)
        self.active_game_db.add_or_update_game(new_game)

    def get_town_ids_for_member_id(self, member_id:int) -> set[TownId]:
        return self.town_ids_from_member_id_map[member_id] if member_id in self.town_ids_from_member_id_map else set()

    def assign_or_unassign_town_id_for_members(self, town_id:TownId, member_diff:SetDifference) -> None:
        '''Assigns a game to the new members in a game, unassigns it from the old ones'''
        for member_id in member_diff.removed:
            if member_id in self.town_ids_from_member_id_map:
                self.town_ids_from_member_id_map[member_id].remove(town_id)
                if len(self.town_ids_from_member_id_map[member_id]) == 0:
                    self.town_ids_from_member_id_map.pop(member_id)

        for member_id in member_diff.added:
            if member_id not in self.town_ids_from_member_id_map:
                self.town_ids_from_member_id_map[member_id] = set()
            self.town_ids_from_member_id_map[member_id].add(town_id)


    def on_game_added(self, game:ActiveGame, member_diff:SetDifference) -> None:
        '''To call whenever a game is added'''
        self.current_games[game.town_id] = game
        self.assign_or_unassign_town_id_for_members(game.town_id, member_diff)

class ActiveGameDb(IActiveGameDb):
    '''Mongo implementation of IActiveGameDb'''

    def __init__(self, database:Database, datetime_provider:IDateTimeProvider) -> None:
        self.collection:Collection = database['ActiveGames']
        self.datetime_provider:IDateTimeProvider = datetime_provider

    @staticmethod
    def get_town_id_query_from_game(game:ActiveGame) -> dict:
        return botcmongo.get_town_id_query(game.town_id)

    @staticmethod
    def get_post_from_game(game:ActiveGame, datetime_provider:IDateTimeProvider) -> dict:
        post:dict = ActiveGameDb.get_town_id_query_from_game(game)
        post['storytellerIds'] = list(game.storyteller_ids)
        post['playerIds'] = list(game.player_ids)
        post['lastActivity'] = datetime_provider.now()
        return post

    @staticmethod
    def get_game_from_post(post:dict) -> ActiveGame:
        game = ActiveGame()
        game.town_id = botcmongo.get_town_id_from_post(post)
        game.storyteller_ids = set(post['storytellerIds']) if 'storytellerIds' in post else set()
        game.player_ids = set(post['playerIds']) if 'playerIds' in post else set()
        game.last_activity = post['lastActivity']
        return game

    def retrieve_active_games(self) -> list[ActiveGame]:
        games = self.collection.find()
        return list(map(ActiveGameDb.get_game_from_post, games))

    def add_or_update_game(self, game:ActiveGame) -> None:
        query = ActiveGameDb.get_town_id_query_from_game(game)
        post = ActiveGameDb.get_post_from_game(game, self.datetime_provider)
        self.collection.replace_one(query, post, True)

    def remove_game(self, game:ActiveGame) -> None:
        query = ActiveGameDb.get_town_id_query_from_game(game)
        self.collection.delete_one(query)
