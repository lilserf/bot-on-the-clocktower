'''Module for dealing with mongo'''

from botctypes import TownId

def get_town_id_query(town_id:TownId) -> dict:
    return {
        'guild' : town_id.guild_id,
        'channel' : town_id.channel_id,
    }

def get_town_id_from_post(post:dict) -> TownId:
    return TownId(post['guild'], post['channel'])
