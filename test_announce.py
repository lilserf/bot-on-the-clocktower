import unittest
import discord
import announce
import version

v1embed = discord.Embed(title=f'Version 1.0', description=f'Features for version 1.0')
v2embed = discord.Embed(title=f'Version 2.0', description=f'Features for version 2.0')

class MockAnnouncerDb(announce.IAnnouncerDb):
	def __init__(self):
		self.guild_version_map = {}
		self.called = 0
		self.record_called = 0

	def has_guild_seen_version(self, guild, version):
		self.called += 1
		if guild in self.guild_version_map:
			ver = self.guild_version_map[guild]
			return (version <= ver)
		else:
			return False

	def record_guild_seen_version(self, guild, version):
		self.record_called += 1
		self.guild_version_map[guild] = version

class MockAnnouncerVersionProvider(version.IVersionProvider):
	def __init__(self):
		self.version_embed_map = {}
		self.called = 0
	
	def get_versions_and_embeds(self):
		self.called += 1
		return self.version_embed_map

class MockAnnouncerGuildDb(announce.IAnnouncerGuildDb):
	def __init__(self):
		self.guilds = list()
		self.called = 0

	def get_guilds(self):
		self.called += 1
		return self.guilds

class MockAnnouncerMessageSender(announce.IAnnouncerMessageSender):
	def __init__(self):
		self.called = 0
		self.throw_exception = False

	async def send_embed(self, guild, embed):
		self.called += 1
		if self.throw_exception:
			raise Exception('fake', 'fake')
		pass

class TestAnnounce(unittest.IsolatedAsyncioTestCase):

	def setUp(self):
		self.tdb = MockAnnouncerDb()
		self.tp = MockAnnouncerVersionProvider()
		self.tgdb = MockAnnouncerGuildDb()
		self.ts = MockAnnouncerMessageSender()
		self.a = announce.AnnouncerImpl(self.tdb, self.tgdb, self.tp, self.ts)

	async def test_noguilds(self):

		# no guilds or anything
		await self.a.announce_latest_version()

		# guild list is fetched but nothing else happens
		self.assertEqual(1, self.tgdb.called)
		self.assertEqual(0, self.tdb.called)
		self.assertEqual(0, self.tp.called)
		self.assertEqual(0, self.ts.called)
		self.assertEqual(0, self.tdb.record_called)

	async def test_oneguild_norecord(self):

		self.tgdb.guilds = [ "100" ]
		self.tp.version_embed_map = { (1,0): v1embed }

		await self.a.announce_latest_version()

		# guild list is fetched
		self.assertEqual(1, self.tgdb.called)
		# versions are fetched
		self.assertEqual(1, self.tp.called)
		# guild records are fetched
		self.assertEqual(1, self.tdb.called)
		# message is sent since no record exists
		self.assertEqual(1, self.ts.called)
		self.assertEqual(1, self.tdb.record_called)

	async def test_oneguild_hasseenthisversion(self):

		self.tgdb.guilds = [ "100" ]
		self.tp.version_embed_map = { (1,0): v1embed }
		self.tdb.guild_version_map = { "100" : (1,0)}

		await self.a.announce_latest_version()

		self.assertEqual(1, self.tgdb.called)
		self.assertEqual(1, self.tp.called)
		self.assertEqual(1, self.tdb.called)
		# no message is sent since this guild has seen this version
		self.assertEqual(0, self.ts.called)
		self.assertEqual(0, self.tdb.record_called)

	async def test_oneguild_hasseenlaterversion(self):

		self.tgdb.guilds = [ "100" ]
		self.tp.version_embed_map = { (1,0): v1embed }
		self.tdb.guild_version_map = { "100" : (2,0)}

		await self.a.announce_latest_version()

		self.assertEqual(1, self.tgdb.called)
		self.assertEqual(1, self.tp.called)
		self.assertEqual(1, self.tdb.called)
		# no message is sent since this guild has seen this version
		self.assertEqual(0, self.ts.called)
		self.assertEqual(0, self.tdb.record_called)

	async def test_oneguild_hasnotseenminorversion(self):

		self.tgdb.guilds = [ "100" ]
		self.tp.version_embed_map = { (1,1): v1embed }
		self.tdb.guild_version_map = { "100" : (1,0)}

		await self.a.announce_latest_version()

		self.assertEqual(1, self.tgdb.called)
		self.assertEqual(1, self.tp.called)
		self.assertEqual(1, self.tdb.called)
		# message is sent since minor version is newer
		self.assertEqual(1, self.ts.called)
		self.assertEqual(1, self.tdb.record_called)

	async def test_oneguild_twoversions(self):

		self.tgdb.guilds = [ "100" ]
		self.tp.version_embed_map = { (1,0) : v1embed, (2,0) : v2embed }
		self.tdb.guild_version_map = { "100" : (1,0) }

		await self.a.announce_latest_version()

		self.assertEqual(1, self.tgdb.called)
		self.assertEqual(1, self.tp.called)
		self.assertEqual(2, self.tdb.called)
		# message is sent since version is newer
		self.assertEqual(1, self.ts.called)
		self.assertEqual(1, self.tdb.record_called)

	async def test_twoguild_twoversions(self):

		self.tgdb.guilds = [ "100", "200" ]
		self.tp.version_embed_map = { (1,0) : v1embed, (2,0) : v2embed }
		self.tdb.guild_version_map = { "100" : (1,0) }

		await self.a.announce_latest_version()

		self.assertEqual(1, self.tgdb.called)
		self.assertEqual(1, self.tp.called)
		self.assertEqual(4, self.tdb.called)
		# message is sent since version is newer
		self.assertEqual(3, self.ts.called)
		self.assertEqual(3, self.tdb.record_called)

	async def test_twoguild_sendembedexception(self):

		self.tgdb.guilds = [ "100" ]
		self.tp.version_embed_map = { (1,0) : v1embed, (2,0) : v2embed }
		self.tdb.guild_version_map = { "100" : (1,0) }
		self.ts.throw_exception = True

		await self.a.announce_latest_version()

		self.assertEqual(1, self.tgdb.called)
		self.assertEqual(1, self.tp.called)
		self.assertEqual(2, self.tdb.called)
		# message is sent since version is newer, and should throw exception
		self.assertEqual(1, self.ts.called)
		# should have recorded this version despite the exception
		self.assertEqual(1, self.tdb.record_called)

