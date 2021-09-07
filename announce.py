import version

class IAnnouncerDb:
	def has_guild_seen_version(self, guild, version):
		pass

class IAnnouncerGuildDb:
	def get_guilds():
		pass

class IAnnouncerMessageSender:
	def send_embed(self, guild, embed):
		pass

class AnnouncerDbImpl(IAnnouncerDb):
	def __init__(self, mongo):
		self.collection = mongo['GuildVersionAnnouncements']

	def has_guild_seen_version(self, guild, version):
		query = { "guild" : guild }
		doc = self.collection.find_one(query)
		if doc:
			vers = tuple(doc['version'])
			return (version <= vers)
		else:
			return False

	def record_guild_seen_version(self, guild, version):
		query = {"guild" : guild }
		doc = self.collection.find_one(query)
		if doc:
			doc['version'] = list(version)
		else:
			doc = {"guild" : guild, "version" : list(version)}
		self.collection.replace_one(query, doc, True)


class AnnouncerGuildDbImpl(IAnnouncerGuildDb):
	def __init__(self, mongo):
		self.collection = mongo['GuildInfo']

	def get_guilds(self):
		x = self.collection.find(projection={'guild':True, '_id':False}).distinct('guild')
		return x

class AnnouncerMessageSenderImpl(IAnnouncerMessageSender):
	def __init__(self, bot, mongo):
		self.bot = bot
		self.collection = mongo['GuildInfo']

	async def send_embed(self, guild, embed):
		query = {"guild": guild}
		result = self.collection.find(query)
		for x in result:
			townInfo = self.bot.getTownInfoByIds(guild, x['controlChannelId'])
			await townInfo.controlChannel.send(embed=embed)

class AnnouncerImpl:
	def __init__(self, db, guildDb, provider, sender):
		self.db = db
		self.guildDb = guildDb
		self.provider = provider
		self.sender = sender

	def guild_no_announce(self, guild):
		impossiblyLargeVersion = (999999,0,0)
		self.db.record_guild_seen_version(guild, impossiblyLargeVersion)

	async def announce_latest_version(self):
		guilds = self.guildDb.get_guilds()
		numSent = 0

		if len(guilds) > 0:
			versions = self.provider.get_versions_and_embeds()

			for g in guilds:
				for (v, embed) in versions.items():
					if not self.db.has_guild_seen_version(g, v):
						self.db.record_guild_seen_version(g, v)
						numSent += 1
						try:
							await self.sender.send_embed(g, embed)
						except Exception:
							# If sending embed fails, do nothing
							pass

		return numSent;

# Concrete class for use by the cog
class Announcer:
	def __init__(self, bot, mongo):
		db = AnnouncerDbImpl(mongo)
		guildDb = AnnouncerGuildDbImpl(mongo)
		provider = version.VersionProviderImpl()
		sender = AnnouncerMessageSenderImpl(bot, mongo)

		self.impl = AnnouncerImpl(db, guildDb, provider, sender)

	def guild_no_announce(self, guild):
		self.impl.guild_no_announce(guild)

	async def announce_latest_version(self):
		return await self.impl.announce_latest_version()
