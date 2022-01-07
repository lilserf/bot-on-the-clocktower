using Bot.Api;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpGameSlashCommands : SlashCommandModule
    {
        public IBotGameplay? BotGameplay { get; set; }
        public IBotVoteTimer? BotVoteTimer { get; set; }

        [SlashCommand("game", "Starts up a game of Blood on the Clocktower")]
        public Task GameCommand(InteractionContext ctx) => BotGameplay!.CommandGameAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("night", "Move all active players from Town Square into Cottages for the night")]
        public Task NightCommand(InteractionContext ctx) => BotGameplay!.CommandNightAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("day", "Move all active players from Cottages to Town Square")]
        public Task DayCommand(InteractionContext ctx) => BotGameplay!.CommandDayAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("vote", "Move all active players to Town Square for voting")]
        public Task VoteCommand(InteractionContext ctx) => BotGameplay!.CommandVoteAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("voteTimer", "Move all active players to Town Square for voting after a provided amount of time")]
        public Task VoteTimerCommand(InteractionContext ctx,
            [Option("timeString", "Time string, such as \"5m30s\" or \"2 minutes\". Valid times are between 10 seconds and 20 minutes.")] string timeString)
            => BotVoteTimer!.RunVoteTimerAsync(new DSharpInteractionContext(ctx), timeString);

        [SlashCommand("endGame", "End any current game, removing roles etc")]
        public Task EndGameCommand(InteractionContext ctx) => BotGameplay!.CommandEndGameAsync(new DSharpInteractionContext(ctx));
    }
}
