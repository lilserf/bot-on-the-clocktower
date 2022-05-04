using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IGameMetricRecord
    {
        int TownHash { get; }

        DateTime FirstActivity { get; }
        DateTime LastActivity { get; }

        bool Complete { get; }
        int Days { get; }
        int Nights { get; }
        int Votes { get; }
    }
}
