using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Callbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class Announcer : IAnnouncer
    {
        static List<ulong> s_guildAllowList = new List<ulong>()
        {
            128585855097896963,
        };

        IVersionProvider m_versionProvider;
        IAnnouncementDatabase m_announcementDatabase;
        ITownDatabase m_townDatabase;
        IBotSystem m_botSystem;
        IBotClient m_botClient;
        IDateTime m_dateTime;
        ITownMaintenance m_townMaintenance;
        IEnvironment m_environment;

        public Announcer(IServiceProvider sp)
        {
            sp.Inject(out m_versionProvider);
            sp.Inject(out m_announcementDatabase);
            sp.Inject(out m_townDatabase);
            sp.Inject(out m_botSystem);
            sp.Inject(out m_botClient);
            sp.Inject(out m_dateTime);     
            sp.Inject(out m_townMaintenance);
            sp.Inject(out m_environment);

            m_townMaintenance.AddMaintenanceTask(AnnounceToTown);
        }

        private async Task AnnounceToTown(TownKey townKey)
        {
            if(!bool.TryParse(m_environment.GetEnvironmentVariable("RESTRICT_ANNOUNCE"), out bool restricted))
            {
                restricted = false;
            }

            // If we're restricted, skip all guilds not in the allowList
            if (restricted && !s_guildAllowList.Contains(townKey.GuildId))
                return;

            Serilog.Log.Information("AnnounceToTown: Checking town {townKey}", townKey);

            var guild = await m_botClient.GetGuildAsync(townKey.GuildId);
            if (guild == null) 
                return;

            var chan = guild.GetChannel(townKey.ControlChannelId);
            if (chan == null) 
                return;

            foreach (var versionObj in m_versionProvider.Versions)
            {
                Serilog.Log.Information("AnnounceToTowns: Checking version {versionObj}", versionObj);
                if (!await m_announcementDatabase.HasSeenVersion(townKey.GuildId, versionObj.Key))
                {
                    await chan.SendMessageAsync(versionObj.Value);
                    await m_announcementDatabase.RecordGuildHasSeenVersion(townKey.GuildId, versionObj.Key);
                }
            }
        }


        public Task SetGuildAnnounce(ulong guildId, bool announce)
        {
            if (announce)
            {
                var latestVersion = m_versionProvider.Versions.Last();
                return m_announcementDatabase.RecordGuildHasSeenVersion(guildId, latestVersion.Key, true);
            }
            else
            {
                return m_announcementDatabase.RecordGuildHasSeenVersion(guildId, IAnnouncementRecord.ImpossiblyLargeVersion, true);
            }
        }

        public async Task CommandSetGuildAnnounce(IBotInteractionContext ctx, bool hear)
        {
            await ctx.DeferInteractionResponse();

            await SetGuildAnnounce(ctx.Guild.Id, hear);

            string onOff = hear ? "on" : "off";
            var builder = m_botSystem.CreateWebhookBuilder().WithContent($"Turned announcements **{onOff}** for this server.");
            await ctx.EditResponseAsync(builder);
        }
    }
}
