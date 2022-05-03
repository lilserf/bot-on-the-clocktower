using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class ShutdownService : IFinalShutdownService, IShutdownPreventionService
    {
        private readonly IDateTime m_dateTime;

        private readonly List<Task> m_shutdownPreventers = new();
        private readonly TaskCompletionSource m_readyToShutdownTcs = new();

        private bool m_shutdownRequested = false;

        public ShutdownService(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            serviceProvider.Inject(out m_dateTime);
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
                var beginTime = m_dateTime.Now;
                Serilog.Log.Debug($"Shutdown requested at {beginTime}");

                m_shutdownRequested = true;
                ShutdownRequested?.Invoke(this, EventArgs.Empty);

                if (m_shutdownPreventers.Count > 0)
                    Task.WhenAll(m_shutdownPreventers).ContinueWith(_ =>
                    {
                        SetReadyToShutdown(beginTime);
                        return Task.CompletedTask;
                    });
                else
                    SetReadyToShutdown(beginTime);
            }
        }

        private void SetReadyToShutdown(DateTime beginTime)
        {
            var now = m_dateTime.Now;
            if (m_readyToShutdownTcs.TrySetResult())
                Serilog.Log.Information($"Shutdown requested at {beginTime} actions completed after {now - beginTime}");
        }
    }
}
