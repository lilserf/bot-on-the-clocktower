using Bot.Api;
using Bot.Api.Database;
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
                readyTcs.TrySetResult();
                return Task.CompletedTask;
            };

			m_discord.ComponentInteractionCreated += ComponentInteractionCreated;

            await m_discord.ConnectAsync();

            await readyTcs.Task;
        }

		private Task ComponentInteractionCreated(IDiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
		{
            return m_componentService.CallAsync(new DSharpComponentContext(e.Interaction));
        }

        public async Task<IChannel> GetChannelAsync(ulong id)
		{
            return new DSharpChannel(await m_discord.GetChannelAsync(id));
		}

        public async Task<IChannelCategory> GetChannelCategoryAsync(ulong id)
		{
            return new DSharpChannelCategory(await m_discord.GetChannelAsync(id));
		}

		public async Task<IGuild> GetGuildAsync(ulong id)
		{
            return new DSharpGuild(await m_discord.GetGuildAsync(id));
		}

        public async Task<ITown?> ResolveTownAsync(ITownRecord rec)
        {
            var guild = await GetGuildAsync(rec.GuildId);
            if (guild != null)
            {
                var town = new Town(rec)
                {
                    Guild = guild,
                    ControlChannel = await GetChannelAsync(rec.ControlChannelId),
                    DayCategory = await GetChannelCategoryAsync(rec.DayCategoryId),
                    NightCategory = await GetChannelCategoryAsync(rec.NightCategoryId),
                    ChatChannel = await GetChannelAsync(rec.ChatChannelId),
                    TownSquare = await GetChannelAsync(rec.TownSquareId),
                    StorytellerRole = guild.Roles[rec.StorytellerRoleId],
                    VillagerRole = guild.Roles[rec.VillagerRoleId],
                };
                return town;
            }
            return null;
        }

        public async Task<IGuild> GetGuild(ulong guildId)
        {
            return new DSharpGuild(await m_discord.GetGuildAsync(guildId));
        }

        public class InvalidDiscordTokenException : Exception { }
    }
}
