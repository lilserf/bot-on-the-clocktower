using Bot.Api;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpGameSlashCommands : SlashCommandModule
    {
        public IBotGameService? BotGameService { get; set; }

        [SlashCommand("game", "Starts up a game of Blood on the Clocktower")]
        public Task GameCommand(InteractionContext ctx) => BotGameService!.RunGameAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("night", "Move to night")]
        public Task NightCommand(InteractionContext ctx) => BotGameService!.PhaseNightAsync(new DSharpInteractionContext(ctx));
    }
}
