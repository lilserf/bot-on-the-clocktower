using Bot.Api;
using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class Announcer : IAnnouncer
    {
        private readonly static List<ulong> s_guildAllowList = new()
        {
            128585855097896963,
        };

        private readonly IVersionProvider m_versionProvider;
        private readonly IAnnouncementDatabase m_announcementDatabase;
        private readonly IBotSystem m_botSystem;
        private readonly IBotClient m_botClient;
        private readonly ITownMaintenance m_townMaintenance;
        private readonly IEnvironment m_environment;

        public Announcer(IServiceProvider sp)
        {
            sp.Inject(out m_versionProvider);
            sp.Inject(out m_announcementDatabase);
            sp.Inject(out m_botSystem);
            sp.Inject(out m_botClient);
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

            Serilog.Log.Verbose("AnnounceToTown: Checking town {townKey}", townKey);

            var guild = await m_botClient.GetGuildAsync(townKey.GuildId);
            if (guild == null) 
                return;

            var chan = guild.GetChannel(townKey.ControlChannelId);
            if (chan == null) 
                return;

            foreach (var versionObj in m_versionProvider.Versions)
            {
                Serilog.Log.Verbose("AnnounceToTowns: Checking version {versionObj}", versionObj.Key);
                if (!await m_announcementDatabase.HasSeenVersion(townKey.GuildId, versionObj.Key))
                {
                    Serilog.Log.Information("AnnounceToTowns: Sending message about version {versionObj} to town {townKey}", versionObj.Key, townKey);
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
