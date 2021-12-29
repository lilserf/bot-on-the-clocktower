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
        private readonly IServiceProvider m_serviceProvider;
        private readonly IEnvironment m_environment;

        public DSharpClient(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
            m_environment = serviceProvider.GetService<IEnvironment>();
        }

        public Task ConnectAsync()
        {
            var token = m_environment.GetEnvironmentVariable("DISCORD_TOKEN");

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
                com.SetClientContext(this, m_serviceProvider);

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
                    if (m_client == null) throw new InvalidOperationException("Must set up client context before accepting commands");
                    return m_client!;
                }
            }

            protected IServiceProvider Services
            {
                get
                {
                    if (m_services == null) throw new InvalidOperationException("Must set up client context before accepting commands");
                    return m_services!;
                }
            }

            private IBotClient? m_client = null;
            private IServiceProvider? m_services = null;

            public void SetClientContext(IBotClient client, IServiceProvider serviceProvider)
            {
                m_client = client;
                m_services = serviceProvider;
            }
        }

        private interface ICommandWithClientContext
        {
            void SetClientContext(IBotClient client, IServiceProvider serviceProvider);
        }

        public class InvalidDiscordTokenException : Exception { }
    }
}
