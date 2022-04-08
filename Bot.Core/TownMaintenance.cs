﻿using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Callbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    internal class TownMaintenance : ITownMaintenance
    {
        const int NUM_TOWNS_PER_CALLBACK = 5;
        const int MINUTES_PER_CALLBACK = 5;

        private readonly ICallbackScheduler<Queue<TownKey>> m_callbackScheduler;
        private readonly ITownDatabase m_townDatabase;
        private readonly IBotClient m_botClient;
        private readonly IDateTime m_dateTime;
        private readonly IShutdownPreventionService m_shutdownPrevention;

        private readonly TaskCompletionSource m_shutdownTcs = new();
        private bool m_shutdownRequested = false;
        private bool m_queueProcessing = false;

        List<Func<TownKey, Task>> m_startupTasks = new();

        public TownMaintenance(IServiceProvider sp)
        {
            sp.Inject(out m_dateTime);
            sp.Inject(out m_townDatabase);
            sp.Inject(out m_botClient);
            sp.Inject(out m_shutdownPrevention);

            m_shutdownPrevention.ShutdownRequested += (s, e) =>
            {
                m_shutdownRequested = true;
                if (!m_queueProcessing)
                {
                    m_shutdownTcs.TrySetResult();
                }
            };
            m_shutdownPrevention.RegisterShutdownPreventer(m_shutdownTcs.Task);

            var callbackFactory = sp.GetService<ICallbackSchedulerFactory>();
            m_callbackScheduler = callbackFactory.CreateScheduler<Queue<TownKey>>(ProcessQueue, TimeSpan.FromMinutes(1));

            m_botClient.Connected += async (o, args) => await RunMaintenance();
        }

        private async Task ProcessQueue(Queue<TownKey> towns)
        {
            m_queueProcessing = true;
            Serilog.Log.Information("TownMaintenance: {townCount} towns remain...", towns.Count());

            int numSent = 0;
            while (numSent < NUM_TOWNS_PER_CALLBACK && towns.Count > 0)
            {
                var townKey = towns.Dequeue();

                Serilog.Log.Information("TownMaintenance: Processing town {townKey}", townKey);

                foreach (var task in m_startupTasks)
                {
                    await task(townKey);
                }
                numSent++;
            }

            if (towns.Count > 0)
            {
                if (m_shutdownRequested)
                    m_shutdownTcs.TrySetResult();
                else
                {
                    Serilog.Log.Information("TownMaintenance: {townCount} towns remain, scheduling callback...", towns.Count);
                    DateTime nextTime = m_dateTime.Now + TimeSpan.FromMinutes(MINUTES_PER_CALLBACK);
                    m_callbackScheduler.ScheduleCallback(towns, nextTime);
                }
            }
            else
            {
                Serilog.Log.Information("TownMaintenance: maintenance complete!");
            }

            m_queueProcessing = false;
        }

        public void AddMaintenanceTask(Func<TownKey, Task> startupTask)
        {
            m_startupTasks.Add(startupTask);
        }

        public async Task RunMaintenance()
        {
            var allTowns = new Queue<TownKey>(await m_townDatabase.GetAllTowns());

            await ProcessQueue(allTowns);
        }
    }
}
