using Bot.Api;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpClient : IBotClient
    {
        private readonly IEnvironment mEnvironment;

        public DSharpClient(IServiceProvider serviceProvider)
        {
            mEnvironment = serviceProvider.GetService<IEnvironment>();
        }

        public Task ConnectAsync()
        {
            var token = mEnvironment.GetEnvironmentVariable("DISCORD_TOKEN");

            if (string.IsNullOrWhiteSpace(token)) throw new InvalidDiscordTokenException();

            // NOTE: The below is not tested
            var config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            };

            var discord = new DiscordClient(config);
            var slash = discord.UseSlashCommands();

            slash.RegisterCommands<SlashCommands>(128585855097896963);

            discord.Ready += Discord_Ready;

            return discord.ConnectAsync();
        }

        private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        class SlashCommands : SlashCommandModule
        {
            [SlashCommand("test", "A test C# slash command")]
            public async Task TestCommand(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

                var builder = new DiscordWebhookBuilder().WithContent("Wahoo, this worked!");
                await ctx.EditResponseAsync(builder);
            }
        }

        public class InvalidDiscordTokenException : Exception { }
    }
}
