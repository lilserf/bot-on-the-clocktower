using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IGameActivityDatabase
    {
        public Task<IGameActivityRecord> GetActivityRecord(TownKey townKey);

        public Task<IEnumerable<IGameActivityRecord>> GetAllActivityRecords();

        public Task RecordActivityAsync(TownKey townKey, DateTime activityTime);

        public Task ClearActivityAsync(TownKey townKey);
    }
}
