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
        private DiscordClient? m_discord;

        public DSharpClient(IServiceProvider serviceProvider)
        {
            ServiceProvider sp = new(serviceProvider);
            sp.AddService<IBotClient>(this);
            m_serviceProvider = sp;

            m_environment = serviceProvider.GetService<IEnvironment>();
            m_discord = null;
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

            m_discord = new DiscordClient(config);
            var slash = m_discord.UseSlashCommands(new SlashCommandsConfiguration { Services = m_serviceProvider });

            // TODO: register to all guilds, not just ours
            slash.RegisterCommands<DSharpGameSlashCommands>(128585855097896963);

            TaskCompletionSource readyTcs = new();

            m_discord.Ready += (_, _) =>
            {
                readyTcs.SetResult();
                return Task.CompletedTask;
            };

			m_discord.ComponentInteractionCreated += ComponentInteractionCreated;

            await m_discord.ConnectAsync();

            await readyTcs.Task;
        }

		private async Task ComponentInteractionCreated(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
		{
            // TODO: a registry of some sort so that BotGameplay can register buttons and lambdas to call when they're pushed
            var builder = new DiscordInteractionResponseBuilder().WithContent("You clicked on my button. Congratulations!");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, builder);
		}

		public async Task<IChannel> GetChannelAsync(ulong id)
		{
            return new DSharpChannel(await m_discord!.GetChannelAsync(id));
		}

		public async Task<IGuild> GetGuildAsync(ulong id)
		{
            return new DSharpGuild(await m_discord!.GetGuildAsync(id));
		}

        public async Task<ITown> ResolveTownAsync(ITownRecord rec)
        {
            var guild = await GetGuildAsync(rec.GuildId);
            var town = new DSharpTown()
            {
                Guild = guild,
                ControlChannel = await GetChannelAsync(rec.ControlChannelId),
                DayCategory = await GetChannelAsync(rec.DayCategoryId),
                NightCategory = await GetChannelAsync(rec.NightCategoryId),
                ChatChannel = await GetChannelAsync(rec.ChatChannelId),
                TownSquare = await GetChannelAsync(rec.TownSquareId),
                StoryTellerRole = guild.Roles[rec.StoryTellerRoleId],
                VillagerRole = guild.Roles[rec.VillagerRoleId],
            };
            return town;
		}

		public class InvalidDiscordTokenException : Exception { }
    }
}
