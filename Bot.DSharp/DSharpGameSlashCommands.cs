using Bot.Api;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class EmptyCommands : SlashCommandModule
    {
    }

    internal class DSharpGameSlashCommands : SlashCommandModule
    {
        public IBotGameplayInteractionHandler? BotGameplayHandler { get; set; }

        [SlashCommand("game", "Starts up a game of Blood on the Clocktower")]
        public Task GameCommand(InteractionContext ctx) => BotGameplayHandler!.CommandGameAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("night", "Move all active players from Town Square into Cottages for the night")]
        public Task NightCommand(InteractionContext ctx) => BotGameplayHandler!.CommandNightAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("day", "Move all active players from Cottages to Town Square")]
        public Task DayCommand(InteractionContext ctx) => BotGameplayHandler!.CommandDayAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("vote", "Move all active players to Town Square for voting")]
        public Task VoteCommand(InteractionContext ctx) => BotGameplayHandler!.CommandVoteAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("voteTimer", "Move all active players to Town Square for voting after a provided amount of time")]
        public Task VoteTimerCommand(InteractionContext ctx,
            [Option("timeString", "Time string, such as \"5m30s\" or \"2 minutes\". Valid times are between 10 seconds and 20 minutes.")] string timeString)
            => BotGameplayHandler!.RunVoteTimerAsync(new DSharpInteractionContext(ctx), timeString);

        [SlashCommand("stopVoteTimer", "Cancels an outstanding call to `/voteTimer`.")]
        public Task StopVoteTimerCommand(InteractionContext ctx) => BotGameplayHandler!.RunStopVoteTimerAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("endGame", "End any current game, removing roles etc")]
        public Task EndGameCommand(InteractionContext ctx) => BotGameplayHandler!.CommandEndGameAsync(new DSharpInteractionContext(ctx));

        [SlashCommand("storytellers", "Explicitly list which users should be Storytellers")]
        public Task StorytellersCommand(InteractionContext ctx,
            [Option("user1", "Storyteller")] DiscordUser user1,
            [Option("user2", "Further storyteller")] DiscordUser? user2 = null,
            [Option("user3", "An additional storyteller")] DiscordUser? user3 = null,
            [Option("user4", "Yet another storyteller")] DiscordUser? user4 = null,
            [Option("user5", "Hopefully the last storyteller")] DiscordUser? user5 = null
            )
        {
            var allUsers = new[] { user1, user2, user3, user4, user5 };

            return BotGameplayHandler!.CommandSetStorytellersAsync(new DSharpInteractionContext(ctx), allUsers.Where(x => x != null).Cast<DiscordMember>().Select(x => new DSharpMember(x)).ToList());
        }
    }
}
