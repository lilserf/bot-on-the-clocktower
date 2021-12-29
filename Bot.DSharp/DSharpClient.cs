using Bot.Api;
using Bot.Base;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpClient : IBotClient
    {
        private readonly IServiceProvider m_serviceProvider;
        private readonly IEnvironment m_environment;

        public DSharpClient(IServiceProvider serviceProvider)
        {
            ServiceProvider sp = new(serviceProvider);
            sp.AddService<IBotClient>(this);
            m_serviceProvider = sp;

            m_environment = serviceProvider.GetService<IEnvironment>();
        }

        public async Task ConnectAsync()
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
            var slash = discord.UseSlashCommands(new SlashCommandsConfiguration { Services = m_serviceProvider });

            slash.RegisterCommands<DSharpGameSlashCommands>(128585855097896963);

            TaskCompletionSource readyTcs = new();

            discord.Ready += (_, _) =>
            {
                readyTcs.SetResult();
                return Task.CompletedTask;
            };

            await discord.ConnectAsync();

            await readyTcs.Task;
        }

        public class InvalidDiscordTokenException : Exception { }
    }
}
