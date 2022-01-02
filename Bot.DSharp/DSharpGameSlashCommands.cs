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

        [SlashCommand("night", "Move all active players from Town Square into Cottages for the night")]
        public Task NightCommand(InteractionContext ctx) => BotGameplay!.PhaseNightAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("day", "Move all active players from Cottages to Town Square")]
        public Task DayCommand(InteractionContext ctx) => BotGameplay!.PhaseDayAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("vote", "Move all active players to Town Square for voting")]
        public Task VoteCommand(InteractionContext ctx) => BotGameplay!.PhaseVoteAsync(new DSharpInteractionContext(ctx));
    }
}
