using Bot.Api;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpGameSlashCommands : SlashCommandModule
    {
        public IBotGameplay? BotGameplay { get; set; }

        [SlashCommand("game", "Starts up a game of Blood on the Clocktower")]
        public Task GameCommand(InteractionContext ctx) => BotGameplay!.RunGameAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("night", "Move to night")]
        public Task NightCommand(InteractionContext ctx) => BotGameplay!.PhaseNightAsync(new DSharpInteractionContext(ctx));
    }
}
