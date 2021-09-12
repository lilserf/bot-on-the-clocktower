import version
'''Classes for dealing with new version announcements'''
# pylint: disable=missing-class-docstring, missing-function-docstring, invalid-name, broad-except

class IAnnouncerDb:
    def has_guild_seen_version(self, guild, ver):
        pass

class IAnnouncerGuildDb:
    def get_guilds(self):
        pass

class IAnnouncerMessageSender:
    async def send_embed(self, guild, embed):
        pass

class AnnouncerDbImpl(IAnnouncerDb):
    def __init__(self, mongo):
        self.collection = mongo['GuildVersionAnnouncements']

    def has_guild_seen_version(self, guild, ver):
        query = { "guild" : guild }
        doc = self.collection.find_one(query)
        if doc:
            vers = tuple(doc['version'])
            return ver<= vers
        return False

    def record_guild_seen_version(self, guild, ver, force=False):
        query = {"guild" : guild }
        doc = self.collection.find_one(query)
        # Create new record if needed
        doc = doc or {"guild" : guild, "version" : list(ver)}

        currVer = tuple(doc['version'])
        update = force or ver > currVer

        if update:
            doc['version'] = list(ver)

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
            town_info = self.bot.getTownInfoByIds(guild, x['controlChannelId'])
            await town_info.control_channel.send(embed=embed)

class AnnouncerImpl:
    def __init__(self, db, guildDb, provider, sender):
        self.db = db
        self.guildDb = guildDb
        self.provider = provider
        self.sender = sender

    def guild_no_announce(self, guild):
        impossiblyLargeVersion = (999999,0,0)
        # Record this version and force it
        self.db.record_guild_seen_version(guild, impossiblyLargeVersion, True)

    def set_to_latest_version(self, guild, force=False):
        versions = self.provider.get_versions_and_embeds()
        (latest_version, _) = list(versions.items())[-1]
        self.db.record_guild_seen_version(guild, latest_version, force)

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

        return numSent

# Concrete class for use by the cog
class Announcer:
    def __init__(self, bot, mongo):
        db = AnnouncerDbImpl(mongo)
        guildDb = AnnouncerGuildDbImpl(mongo)
        provider = version.VersionProviderImpl()
        sender = AnnouncerMessageSenderImpl(bot, mongo)

        self.impl = AnnouncerImpl(db, guildDb, provider, sender)

    def set_to_latest_version(self, guild):
        self.impl.set_to_latest_version(guild)

    def guild_no_announce(self, guild):
        self.impl.guild_no_announce(guild)

    def guild_yes_announce(self, guild):
        self.impl.set_to_latest_version(guild, True)

    async def announce_latest_version(self):
        return await self.impl.announce_latest_version()
