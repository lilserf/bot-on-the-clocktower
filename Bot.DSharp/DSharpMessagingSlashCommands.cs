using Bot.Api;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    class DSharpMessagingSlashCommands : ApplicationCommandModule
    {
        public IBotMessaging? BotMessaging{ get; set; }

        [SlashCommand("evil", "Send a message informing the evil team of each other")]
        public Task EvilCommand(InteractionContext ctx, 
            [Option("demon", "The demon for this game")]DiscordUser demon,
            [Option("minion1", "A minion for this game")]DiscordUser minion1,
            [Option("minion2", "A minion for this game (optional)")]DiscordUser? minion2 = null,
            [Option("minion3", "A minion for this game (optional)")] DiscordUser? minion3 = null,
            [Option("magician", "If a Magician is in this game, specify them here (optional)")] DiscordUser? magician = null)
        {
            var allMinions = new[] { minion1, minion2, minion3 };

            return BotMessaging!.CommandEvilMessageAsync(
                new DSharpInteractionContext(ctx),
                new DSharpMember((DiscordMember)demon), 
                allMinions.Where(x => x != null).Cast<DiscordMember>().Select(x => new DSharpMember(x)).ToList(), 
                magician == null ? null : new DSharpMember((DiscordMember)magician));
        }

        [SlashCommand("lunatic", "Send a message to the Lunatic that *looks* like they're the demon")]
        public Task LunaticCommand(InteractionContext ctx,
            [Option("lunatic", "The Lunatic!")]DiscordUser lunatic,
            [Option("fakeMinion1", "A fake minion player")]DiscordUser fakeMinion1,
            [Option("fakeMinion2", "A fake minion player")] DiscordUser? fakeMinion2 = null,
            [Option("fakeMinion3", "A fake minion player")] DiscordUser? fakeMinion3 = null)
        {
            var allMinions = new[] { fakeMinion1, fakeMinion2, fakeMinion3 };

            return BotMessaging!.CommandLunaticMessageAsync(
                new DSharpInteractionContext(ctx),
                new DSharpMember((DiscordMember)lunatic),
                allMinions.Where(x => x != null).Cast<DiscordMember>().Select(x => new DSharpMember(x)).ToList());
        }

    }
}
