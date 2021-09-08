import discord

async def verify_not_dm_or_send_error(ctx):
    if isinstance(ctx.channel, discord.DMChannel):
        await ctx.send(f"Whoops, you probably meant to send that in a text channel instead of a DM!")
        return False
    return True