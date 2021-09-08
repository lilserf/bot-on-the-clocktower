import discord
import traceback

async def verify_not_dm_or_send_error(ctx):
    if isinstance(ctx.channel, discord.DMChannel):
        await ctx.send(f"Whoops, you probably meant to send that in a text channel instead of a DM!")
        return False
    return True

async def send_error_to_author(ctx, error=None):
    if error:
        formatted = error
    else:
        formatted = '```\n' + traceback.format_exc(3) + '\n```'
        traceback.print_exc()
    await ctx.author.send(f"Alas, an error has occurred:\n{formatted}\n(from message `{ctx.message.content}`)")