#if DEBUG
#define TEST_RAPID_CLEANUP
#endif

using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Callbacks;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class TownCleanup : ITownCleanup
    {
        private readonly ICallbackScheduler<TownKey> m_callbackScheduler;
        private readonly IGameActivityDatabase m_gameActivityDb;
        private readonly IDateTime m_dateTime;
        private readonly IBotClient m_botClient;

        public event EventHandler<TownCleanupRequestedArgs>? CleanupRequested;

        public TownCleanup(IServiceProvider services)
        {
            services.Inject(out m_dateTime);
            services.Inject(out m_gameActivityDb);
            services.Inject(out m_botClient);


            var callbackFactory = services.GetService<ICallbackSchedulerFactory>();

#if TEST_RAPID_CLEANUP
            TimeSpan cleanupCheckTime = TimeSpan.FromSeconds(5);
#else
            TimeSpan cleanupCheckTime = TimeSpan.FromHours(1);
#endif
            m_callbackScheduler = callbackFactory.CreateScheduler<TownKey>(CleanupTown, cleanupCheckTime);

            m_botClient.Connected += BotClient_Connected;
        }

        public async Task RecordActivityAsync(TownKey townKey)
        {
            Serilog.Log.Debug("RecordActivity for town {@townKey}", townKey);
            var now = m_dateTime.Now;
            await m_gameActivityDb.RecordActivityAsync(townKey, now);
            ScheduleCleanup(townKey, now);
        }

        private async Task ScheduleOutstandingCleanup()
        {
            var recs = await m_gameActivityDb.GetAllActivityRecords();
            Serilog.Log.Debug("ScheduleOutstandingCleanup: {numRecords} records found", recs.Count());
            foreach (var rec in recs)
                ScheduleCleanup(new TownKey(rec.GuildId, rec.ChannelId), rec.LastActivity);
        }

        private void ScheduleCleanup(TownKey townKey, DateTime lastActivity)
        {
#if TEST_RAPID_CLEANUP
            TimeSpan cleanupTime = TimeSpan.FromMinutes(1);
#else
            TimeSpan cleanupTime = TimeSpan.FromHours(5);
#endif
            var time = lastActivity + cleanupTime;
            Serilog.Log.Debug("ScheduleCleanup: {townKey} should be cleaned up at {time}", townKey, time);
            m_callbackScheduler.ScheduleCallback(townKey, time);
        }

        private void BotClient_Connected(object? sender, EventArgs e)
        {
            ScheduleOutstandingCleanup().ConfigureAwait(continueOnCapturedContext: true);
        }

        private async Task CleanupTown(TownKey key)
        {
            Serilog.Log.Debug("CleanupTown for town {@townKey}", key);

            try
            {
                CleanupRequested?.Invoke(this, new TownCleanupRequestedArgs(key));
            }
            catch (Exception)
            {
                // Do what?
            }
            finally
            {
                // Success or failure, clear the record
                await m_gameActivityDb.ClearActivityAsync(key);
            }
        }
    }
}
