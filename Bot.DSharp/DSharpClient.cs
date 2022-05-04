using Bot.Api;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpClient : IBotClient
    {
        private readonly IComponentService m_componentService;
        private readonly IEnvironment m_environment;
        private readonly IDiscordClient m_discord;

        public event EventHandler<EventArgs>? Connected;
        public event EventHandler<MessageCreatedEventArgs>? MessageCreated;

        public DSharpClient(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_componentService);
            serviceProvider.Inject(out m_environment);

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

            // If somehow neither deploy type is specified, don't screw around with anything
            string deployType = Environment.GetEnvironmentVariable("DEPLOY_TYPE") ?? "none";

            // The "dev" deploy should only be used by us doing development locally and using the 
            // DEV bot token - so the DEV bot will remain registered only to our servers, with no global
            // commands.
            // The "prod" deploy should only be used by the real server using the real bot token,
            // so it will now be registered globally
            if (deployType == "dev")
            {
                // During development, register our commands to the dev guild only
                List<ulong> devGuildIds = new() { 128585855097896963ul, 215551375608643586ul };

                foreach (ulong devGuildId in devGuildIds)
                {
                    slash.RegisterCommands<DSharpGameSlashCommands>(devGuildId);
                    slash.RegisterCommands<DSharpMessagingSlashCommands>(devGuildId);
                    slash.RegisterCommands<DSharpLookupSlashCommands>(devGuildId);
                    slash.RegisterCommands<DSharpMiscSlashCommands>(devGuildId);
                    slash.RegisterCommands<DSharpSetupSlashCommands>(devGuildId);
                }
                // During development, register no commands globally
                slash.RegisterCommands<EmptyCommands>();
            }
            else if(deployType == "prod")
            {
                slash.RegisterCommands<DSharpGameSlashCommands>();
                slash.RegisterCommands<DSharpMessagingSlashCommands>();
                slash.RegisterCommands<DSharpLookupSlashCommands>();
                slash.RegisterCommands<DSharpMiscSlashCommands>();
                slash.RegisterCommands<DSharpSetupSlashCommands>();
            }

            TaskCompletionSource readyTcs = new();

            m_discord.Ready += (_, _) =>
            {
                if (readyTcs.TrySetResult())
                    Connected?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            };

            m_discord.MessageCreated += OnMessageCreated;
			m_discord.ComponentInteractionCreated += ComponentInteractionCreated;
            m_discord.ModalSubmitted += ModalSubmitted;

            await m_discord.ConnectAsync();

            await readyTcs.Task;
        }

        private Task OnMessageCreated(IDiscordClient _, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot == false)
            {
                MessageCreated?.Invoke(this, new MessageCreatedEventArgs(new DSharpChannel(e.Channel), e.Message.Content));
            }
            return Task.CompletedTask;
        }

        public Task DisconnectAsync() => m_discord.DisconnectAsync();

        public Task<IGuild?> GetGuildAsync(ulong id) => m_discord.GetGuildAsync(id);

        private Task ComponentInteractionCreated(IDiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {
            return m_componentService.CallAsync(new DSharpComponentContext(e.Interaction));
        }

        private Task ModalSubmitted(IDiscordClient sender, ModalSubmitEventArgs e)
        {
            throw new NotImplementedException();
        }

        public class InvalidDiscordTokenException : Exception { }
    }
}
