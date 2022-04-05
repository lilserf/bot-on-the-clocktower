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
        ICallbackScheduler<Queue<TownKey>> m_callbackScheduler;
        IBotSystem m_botSystem;
        IBotClient m_botClient;
        IDateTime m_dateTime;

        const int NUM_TOWNS_PER_CALLBACK = 5;
        const int MINUTES_PER_CALLBACK = 5;

        public Announcer(IServiceProvider sp)
        {
            sp.Inject(out m_versionProvider);
            sp.Inject(out m_announcementDatabase);
            sp.Inject(out m_townDatabase);
            sp.Inject(out m_botSystem);
            sp.Inject(out m_botClient);
            sp.Inject(out m_dateTime);            

            var callbackFactory = sp.GetService<ICallbackSchedulerFactory>();
            m_callbackScheduler = callbackFactory.CreateScheduler<Queue<TownKey>>(AnnounceToTowns, TimeSpan.FromMinutes(1));

            m_botClient.Connected += OnClientConnected;
        }

        private async void OnClientConnected(object? sender, EventArgs e)
        {
            await AnnounceLatestVersion();
        }

        private async Task AnnounceToTowns(Queue<TownKey> towns)
        {
            bool restricted = bool.Parse(Environment.GetEnvironmentVariable("RESTRICT_ANNOUNCE") ?? "false");

            Serilog.Log.Information("AnnounceToTowns: {townCount} towns remain...", towns.Count());
            int numSent = 0;
            while(numSent < NUM_TOWNS_PER_CALLBACK && towns.Count > 0)
            {
                var townKey = towns.Dequeue();

                // If we're restricted, skip all guilds not in the allowList
                if (restricted && !s_guildAllowList.Contains(townKey.GuildId))
                    continue;

                Serilog.Log.Information("AnnounceToTowns: Checking town {townKey}", townKey);

                var guild = await m_botClient.GetGuildAsync(townKey.GuildId);
                if (guild == null) 
                    continue;

                var chan = guild.GetChannel(townKey.ControlChannelId);
                if (chan == null) 
                    continue;

                foreach (var versionObj in m_versionProvider.Versions)
                {
                    Serilog.Log.Information("AnnounceToTowns: Checking version {versionObj}", versionObj);
                    if (!await m_announcementDatabase.HasSeenVersion(townKey.GuildId, versionObj.Key))
                    {
                        await m_announcementDatabase.RecordGuildHasSeenVersion(townKey.GuildId, versionObj.Key);

                        numSent++;
                        await chan.SendMessageAsync(versionObj.Value);
                    }
                }
            }

            if(towns.Count > 0)
            {
                Serilog.Log.Information("AnnounceToTowns: {townCount} towns remain, scheduling callback...", towns.Count);
                DateTime nextTime = m_dateTime.Now + TimeSpan.FromMinutes(MINUTES_PER_CALLBACK);
                m_callbackScheduler.ScheduleCallback(towns, nextTime);
            }
            else
            {
                Serilog.Log.Information("AnnounceToTowns: Announcements complete!");
            }
        }

        public async Task AnnounceLatestVersion()
        {
            var allTowns = new Queue<TownKey>(await m_townDatabase.GetAllTowns());

            await AnnounceToTowns(allTowns);            
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
