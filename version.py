import discord

class IVersionProvider:
	# Should return a map where the keys are tuples like (2,0,0) for version 2.0.0 and the values are discord Embeds to display for those versions
	def get_versions_and_embeds(self):
		pass

class VersionProviderImpl(IVersionProvider):
	def __init__(self):
		self.map = {};

		# Version 2.0.0
		v2 = discord.Embed(title="New features in Bot on the Clocktower v2.0.0!")
		
		v2.add_field(inline=False, name="Vote Timer", value="You can now use `!votetimer 5m30s` to start a timer for the requested time. Villagers will be warned as the timer gets close, and when it expires, villagers will be pulled to the Town Square channel for a vote as if `!vote` was run.")
		v2.add_field(inline=False, name="Character Lookup", value="The new `!character <name>` command fetches information about a given character from the official sets. You can also use `!addScript <json url>` to tell the bot about JSON script files for your own custom sets. If you're using [Bloodstar Clocktica](https://www.bloodstar.xyz) to manage your scripts, you'll even get links to the almanac!")
		v2.add_field(inline=False, name="Release Announcements", value="If you'd like to opt out of these announcements, use the `!noannounce` command")
		v2.add_field(inline=False, name="Need More?", value="See the `!help` menu, [README](https://github.com/lilserf/bot-on-the-clocktower/blob/release/README.md), and [CHANGELOG](https://github.com/lilserf/bot-on-the-clocktower/blob/release/CHANGELOG.md) for all the details!")
		self.map[(2,0,0)] = v2

	def get_versions_and_embeds(self):
		return self.map
