using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public interface ITownCleanup
    {
        Task RecordActivityAsync(TownKey townKey);

        event EventHandler<TownCleanupRequestedArgs> CleanupRequested;
    }

    public class TownCleanupRequestedArgs : EventArgs
    {
        public TownKey TownKey { get; }

        public TownCleanupRequestedArgs(TownKey townKey)
        {
            TownKey = townKey;
        }
    }
}
