using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IGameMetricDatabase
    {
        public Task RecordGameAsync(TownKey townKey, DateTime timestamp);

        public Task RecordNightAsync(TownKey townKey, DateTime timestamp);

        public Task RecordDayAsync(TownKey townKey, DateTime timestamp);

        public Task RecordVoteAsync(TownKey townKey, DateTime timestamp);

        public Task RecordEndGameAsync(TownKey townKey, DateTime timestamp);

        public Task<DateTime?> GetMostRecentGameAsync(TownKey townKey);
    }
}
