using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Callbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    internal class TownMaintenance : ITownMaintenance
    {
        const int NUM_TOWNS_PER_CALLBACK = 5;
        const int MINUTES_PER_CALLBACK = 5;
        const int DAYS_BETWEEN_MAINTENANCE = 14;

        private readonly ICallbackScheduler<Queue<TownKey>> m_callbackScheduler;
        private readonly ICallbackScheduler m_longWaitScheduler;
        private readonly ITownDatabase m_townDatabase;
        private readonly IBotClient m_botClient;
        private readonly IDateTime m_dateTime;
        private readonly IShutdownPreventionService m_shutdownPrevention;

        private readonly TaskCompletionSource m_shutdownTcs = new();
        private bool m_shutdownRequested = false;
        private bool m_queueProcessing = false;

        private readonly List<Func<TownKey, Task>> m_startupTasks = new();

        public TownMaintenance(IServiceProvider sp)
        {
            sp.Inject(out m_dateTime);
            sp.Inject(out m_townDatabase);
            sp.Inject(out m_botClient);
            sp.Inject(out m_shutdownPrevention);

            m_shutdownPrevention.ShutdownRequested += (s, e) => OnShutdownRequested();
            m_shutdownPrevention.RegisterShutdownPreventer(m_shutdownTcs.Task);

            var callbackFactory = sp.GetService<ICallbackSchedulerFactory>();
            // Scheduler for when we're running the queue - every few minutes, process some more towns
            m_callbackScheduler = callbackFactory.CreateScheduler<Queue<TownKey>>(ProcessQueueAsync, TimeSpan.FromMinutes(1));
            // Scheduler for when we're waiting between maintenance periods - only needs to check every day or so
            m_longWaitScheduler = callbackFactory.CreateScheduler(RunMaintenanceAsync, TimeSpan.FromDays(1));

            m_botClient.Connected += async (o, args) => await RunMaintenanceAsync();
        }

        private void OnShutdownRequested()
        {
            m_shutdownRequested = true;
            if (!m_queueProcessing)
                m_shutdownTcs.TrySetResult();
        }

        private async Task ProcessQueueAsync(Queue<TownKey> towns)
        {
            if (m_queueProcessing)
                return;

            m_queueProcessing = true;
            await ProcessQueueInternalAsync(towns);

            m_queueProcessing = false;
        }

        private async Task ProcessQueueInternalAsync(Queue<TownKey> towns)
        {
            Serilog.Log.Information("TownMaintenance: {townCount} towns remain...", towns.Count);

            await ProcessPartialTownQueueAsync(towns);

            if (m_shutdownRequested)
                m_shutdownTcs.TrySetResult();
            else
                ScheduleNextProcessing(towns);
        }

        private async Task ProcessPartialTownQueueAsync(Queue<TownKey> towns)
        {
            for (int i = 0; i< NUM_TOWNS_PER_CALLBACK && towns.Count > 0; ++i)
            {
                var townKey = towns.Dequeue();

                Serilog.Log.Information("TownMaintenance: Processing town {townKey}", townKey);

                foreach (var task in m_startupTasks)
                {
                    try
                    {
                        await task(townKey);
                    }
                    catch (Exception)
                    {
                        // todo: something?
                    }
                }
            }
        }

        private void ScheduleNextProcessing(Queue<TownKey> towns)
        {
            if (towns.Count > 0)
                ScheduleProcessMoreTowns(towns);
            else
                ScheduleNextMaintenance();
        }

        private void ScheduleProcessMoreTowns(Queue<TownKey> towns)
        {
            Serilog.Log.Information("TownMaintenance: {townCount} towns remain, scheduling callback...", towns.Count);
            DateTime nextTime = m_dateTime.Now + TimeSpan.FromMinutes(MINUTES_PER_CALLBACK);
            m_callbackScheduler.ScheduleCallback(towns, nextTime);
        }

        private void ScheduleNextMaintenance()
        {
            DateTime nextTime = m_dateTime.Now + TimeSpan.FromDays(DAYS_BETWEEN_MAINTENANCE);
            Serilog.Log.Information("TownMaintenance: maintenance complete! Next maintenance will be {time}", nextTime);
            m_longWaitScheduler.ScheduleCallback(nextTime);
        }

        public void AddMaintenanceTask(Func<TownKey, Task> startupTask)
        {
            m_startupTasks.Add(startupTask);
        }

        private async Task RunMaintenanceAsync()
        {
            var allTowns = await m_townDatabase.GetAllTowns();
            var allTownsQueue = new Queue<TownKey>(allTowns);
            await ProcessQueueAsync(allTownsQueue);
        }
    }
}
