using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IShutdownPreventionService
    {
        event EventHandler<EventArgs> ShutdownRequested;

        void RegisterShutdownPreventer(Task taskToAwait);
    }
}
