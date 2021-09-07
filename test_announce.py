import unittest

import announce

class TestAnnounce(unittest.TestCase):

	def test_exists(self):

		class MockAnnouncerDb(announce.IAnnouncerDb):
			def has_guild_seen_version(self, guild, version):
				return False

		class MockAnnouncerVersionProvider(announce.IAnnouncerVersionProvider):
			def get_version_message(self, version):
				if version == "1.0": return "Version 1.0"
				if version == "2.0": return "Version 2.0"

			def get_latest_version(self):
				return "2.0"

			def get_latest_version_message(self):
				return self.get_version_message(self.get_latest_version())

		class MockAnnouncerGuildDb(announce.IAnnouncerGuildDb):
			def get_guilds():
				return [ 100, 200, 300 ]

		db = MockAnnouncerDb()
		provider = MockAnnouncerVersionProvider()
		guildDb = MockAnnouncerGuildDb()

		a = announce.Announcer(db, guildDb, provider)

