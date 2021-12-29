using Bot.Api;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpGameSlashCommands : DSharpSlashCommandModuleWithClientContext
    {
        [SlashCommand("game", "Starts up a game of Blood on the Clocktower")]
        public Task GameCommand(InteractionContext ctx)
        {
            var gs = Services.GetService<IBotGameService>();
            return gs.RunGameAsync(Client, new DSharpInteractionContext(ctx));
        }
    }
}
