using Bot.Api;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpClient : IBotClient
    {
        private readonly IComponentService m_componentService;

        private readonly IDiscordClient m_discord;

        public event EventHandler<EventArgs>? Connected;

        public DSharpClient(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_componentService);

            var environment = serviceProvider.GetService<IEnvironment>();
            var token = environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidDiscordTokenException();

            var config = new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            };

            var discordFactory = serviceProvider.GetService<IDiscordClientFactory>();
            m_discord = discordFactory.CreateClient(config);
        }

        public async Task ConnectAsync(IServiceProvider botServices)
        {
            var slash = m_discord.UseSlashCommands(new SlashCommandsConfiguration { Services = botServices });

            // During development, register our commands to the dev guild only
            slash.RegisterCommands<DSharpGameSlashCommands>(128585855097896963);
            slash.RegisterCommands<DSharpMessagingSlashCommands>(128585855097896963);
            // During development, register no commands globally
            slash.RegisterCommands<EmptyCommands>();

            TaskCompletionSource readyTcs = new();

            m_discord.Ready += (_, _) =>
            {
                if (readyTcs.TrySetResult())
                    Connected?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            };

			m_discord.ComponentInteractionCreated += ComponentInteractionCreated;

            await m_discord.ConnectAsync();

            await readyTcs.Task;
        }

        public Task<IGuild?> GetGuildAsync(ulong id) => m_discord.GetGuildAsync(id);

        private Task ComponentInteractionCreated(IDiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {
            return m_componentService.CallAsync(new DSharpComponentContext(e.Interaction));
        }

        public class InvalidDiscordTokenException : Exception { }
    }
}
