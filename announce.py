

class IAnnouncerDb:
	def has_guild_seen_version(self, guild, version):
		pass

class IAnnouncerGuildDb:
	def get_guilds():
		pass

class IAnnouncerVersionProvider:
	def get_version_message(self, version):
		pass

	def get_latest_version(self):
		pass

	def get_latest_version_message(self):
		return self.get_version_message(self.get_latest_version())

class Announcer:

	def __init__(self, db, guildDb, provider):
		self.db = db
		self.guildDb = guildDb
		self.provider = provider


