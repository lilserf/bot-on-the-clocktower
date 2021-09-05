class TownId:
    def __init__(self, guild_id, channel_id):
        self.guild_id = guild_id
        self.channel_id = channel_id

    def __eq__(self, other):
        return other.guild_id == self.guild_id and other.channel_id == self.channel_id

    def __hash__(self):
        return hash(tuple((self.channel_id, self.guild_id)))
    