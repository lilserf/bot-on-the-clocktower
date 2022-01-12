using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IGameActivityDatabase
    {
        public Task<IGameActivityRecord> GetActivityRecord(TownKey townKey);

        public Task<IEnumerable<IGameActivityRecord>> GetAllActivityRecords();

        public Task RecordActivity(TownKey townKey);

        public Task ClearActivity(TownKey townKey);
    }
}
