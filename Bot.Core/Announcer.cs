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
        IVersionProvider m_versionProvider;
        IAnnouncementDatabase m_announcementDatabase;
        ITownDatabase m_townDatabase;
        ICallbackScheduler<Queue<TownKey>> m_callbackScheduler;
        IBotClient m_botClient;
        IDateTime m_dateTime;

        const int NUM_TOWNS_PER_CALLBACK = 1;
        const int MINUTES_PER_CALLBACK = 1;

        public Announcer(IServiceProvider sp)
        {
            sp.Inject(out m_versionProvider);
            sp.Inject(out m_announcementDatabase);
            sp.Inject(out m_townDatabase);
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
            Serilog.Log.Information("AnnounceToTowns: {townCount} towns remain...", towns.Count());
            int numSent = 0;
            while(numSent < NUM_TOWNS_PER_CALLBACK && towns.Count > 0)
            {
                var townKey = towns.Dequeue();
                Serilog.Log.Information("AnnounceToTowns: Checking town {townKey}", townKey);

                var guild = await m_botClient.GetGuildAsync(townKey.GuildId);
                if (guild == null) continue;

                var chan = guild.GetChannel(townKey.ControlChannelId);
                if (chan == null) continue;

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
                DateTime nextTime = m_dateTime.Now + TimeSpan.FromMinutes(1);
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
    }
}
