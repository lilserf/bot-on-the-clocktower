using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IGameMetricDatabase
    {
        public Task RecordGame(TownKey townKey, DateTime timestamp);

        public Task RecordNight(TownKey townKey, DateTime timestamp);

        public Task RecordDay(TownKey townKey, DateTime timestamp);

        public Task RecordVote(TownKey townKey, DateTime timestamp);

        public Task RecordEndGame(TownKey townKey, DateTime timestamp);
    }
}
