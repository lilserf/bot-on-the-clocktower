
import discord
from botctypes import TownInfo

class SetupImpl:

    def __init__(self, mongo):
        pass

    def make_town_embed(self, info:TownInfo) -> discord.Embed:

        guild = info.guild
        embed = discord.Embed(title=f'{guild.name} // {townInfo.dayCategory.name}', description=f'Created {townInfo.timestamp} by {townInfo.authorName}', color=0xcc0000)
        embed.add_field(name="Control Channel", value=townInfo.controlChannel.name, inline=False)
        embed.add_field(name="Town Square", value=townInfo.townSquare.name, inline=False)
        embed.add_field(name="Chat Channel", value=townInfo.chatChannel and townInfo.chatChannel.name or "<None>", inline=False)
        embed.add_field(name="Day Category", value=townInfo.dayCategory.name, inline=False)
        embed.add_field(name="Night Category", value=townInfo.nightCategory and townInfo.nightCategory.name or "<None>", inline=False)
        embed.add_field(name="Storyteller Role", value=townInfo.storyTellerRole.name, inline=False)
        embed.add_field(name="Villager Role", value=townInfo.villagerRole.name, inline=False)
        return embed
        
 