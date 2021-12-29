using Bot.Api;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpClient : IBotClient
    {
        private readonly IServiceProvider mServiceProvider;
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

            foreach (var com in slash.RegisteredCommands.OfType<ICommandWithClientContext>())
                com.SetClientContext(this, mServiceProvider);

            discord.Ready += Discord_Ready;

            return discord.ConnectAsync();
        }

        private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        public IBotInteractionResponseBuilder CreateInteractionResponseBuilder() => new DSharpInteractionResponseBuilder(new DiscordInteractionResponseBuilder());

        private class SlashCommands : CommandWithClientContext
        {
            [SlashCommand("game", "Starts up a game of Blood on the Clocktower")]
            public Task GameCommand(InteractionContext ctx)
            {
                var gs = Services.GetService<IBotGameService>();
                return gs.RunGameAsync(Client, new DSharpInteractionContext(ctx));
            }
        }

        private class CommandWithClientContext : SlashCommandModule
        {
            protected IBotClient Client
            {
                get
                {
                    if (mClient == null) throw new InvalidOperationException("Must set up client context before accepting commands");
                    return mClient!;
                }
            }

            protected IServiceProvider Services
            {
                get
                {
                    if (mServices == null) throw new InvalidOperationException("Must set up client context before accepting commands");
                    return mServices!;
                }
            }

            private IBotClient? mClient = null;
            private IServiceProvider? mServices = null;

            public void SetClientContext(IBotClient client, IServiceProvider serviceProvider)
            {
                mClient = client;
                mServices = serviceProvider;
            }
        }

        private interface ICommandWithClientContext
        {
            void SetClientContext(IBotClient client, IServiceProvider serviceProvider);
        }

        public class InvalidDiscordTokenException : Exception { }
    }
}
