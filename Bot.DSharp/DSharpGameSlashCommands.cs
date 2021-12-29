using Bot.Api;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpGameSlashCommands : SlashCommandModule
    {
        [SlashCommand("game", "Starts up a game of Blood on the Clocktower")]
        public static Task GameCommand(InteractionContext ctx) => ctx.Services.GetService<IBotGameService>().RunGameAsync(new DSharpInteractionContext(ctx));
    }
}
