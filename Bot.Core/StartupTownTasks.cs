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
    internal class StartupTownTasks : IStartupTownTasks
    {
        const int NUM_TOWNS_PER_CALLBACK = 5;
        const int MINUTES_PER_CALLBACK = 5;

        ICallbackScheduler<Queue<TownKey>> m_callbackScheduler;
        ITownDatabase m_townDatabase;
        IBotClient m_botClient;
        IDateTime m_dateTime;

        List<Func<TownKey, Task>> m_startupTasks = new();

        public StartupTownTasks(IServiceProvider sp)
        {
            sp.Inject(out m_dateTime);
            sp.Inject(out m_townDatabase);
            sp.Inject(out m_botClient);

            var callbackFactory = sp.GetService<ICallbackSchedulerFactory>();
            m_callbackScheduler = callbackFactory.CreateScheduler<Queue<TownKey>>(ProcessQueue, TimeSpan.FromMinutes(1));

            m_botClient.Connected += async (o, args) => await Startup();
        }

        private async Task ProcessQueue(Queue<TownKey> towns)
        {
            Serilog.Log.Information("StartupTownTasks: {townCount} towns remain...", towns.Count());

            int numSent = 0;
            while (numSent < NUM_TOWNS_PER_CALLBACK && towns.Count > 0)
            {
                var townKey = towns.Dequeue();

                Serilog.Log.Information("StartupTownTasks: Processing town {townKey}", townKey);

                foreach (var task in m_startupTasks)
                {
                    await task(townKey);
                }
                numSent++;
            }

            if (towns.Count > 0)
            {
                Serilog.Log.Information("StartupTownTasks: {townCount} towns remain, scheduling callback...", towns.Count);
                DateTime nextTime = m_dateTime.Now + TimeSpan.FromMinutes(MINUTES_PER_CALLBACK);
                m_callbackScheduler.ScheduleCallback(towns, nextTime);
            }
            else
            {
                Serilog.Log.Information("StartupTownTasks: Startup Town Tasks complete!");
            }
        }

        public void AddStartupTask(Func<TownKey, Task> startupTask)
        {
            m_startupTasks.Add(startupTask);
        }

        public async Task Startup()
        {
            var allTowns = new Queue<TownKey>(await m_townDatabase.GetAllTowns());

            await ProcessQueue(allTowns);
        }
    }
}
