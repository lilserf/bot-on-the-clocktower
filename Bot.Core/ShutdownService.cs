using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class ShutdownService : IFinalShutdownService, IShutdownPreventionService
    {
        public Task ReadyToShutdown => throw new NotImplementedException();

        public event EventHandler<EventArgs>? ShutdownRequested;

        public void RegisterShutdownPreventer(Task taskToAwait)
        {
            throw new NotImplementedException();
        }
    }
}
