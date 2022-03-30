using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class ShutdownService : IFinalShutdownService, IShutdownPreventionService
    {
        private readonly List<Task> m_shutdownPreventers = new();

        private readonly TaskCompletionSource m_readyToShutdownTcs = new();

        private bool m_shutdownRequested = false;

        public ShutdownService(CancellationToken cancellationToken)
        {
            cancellationToken.Register(CancelRequested);
        }

        public Task ReadyToShutdown => m_readyToShutdownTcs.Task;

        public event EventHandler<EventArgs>? ShutdownRequested;

        public void RegisterShutdownPreventer(Task taskToAwait)
        {
            m_shutdownPreventers.Add(taskToAwait);
        }

        private void CancelRequested()
        {
            if (!m_shutdownRequested)
            {
                Serilog.Log.Debug("A shutdown has been requested.");
                m_shutdownRequested = true;
                ShutdownRequested?.Invoke(this, EventArgs.Empty);

                if (m_shutdownPreventers.Count > 0)
                    Task.WhenAll(m_shutdownPreventers).ContinueWith(_ =>
                    {
                        m_readyToShutdownTcs.TrySetResult();
                        return Task.CompletedTask;
                    });
                else
                    m_readyToShutdownTcs.TrySetResult();
            }
        }
    }
}
