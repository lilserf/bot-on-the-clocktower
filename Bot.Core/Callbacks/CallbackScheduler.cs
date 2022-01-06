using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core.Callbacks
{
    public class CallbackScheduler<TKey> : ICallbackScheduler<TKey>
    {
        private readonly Func<TKey, Task> m_callback;
        private readonly TimeSpan m_period;

        private readonly Dictionary<TKey, DateTime> m_keysToCallbackTime = new();

        private bool m_isPumping = false;
        private readonly object m_pumpLock = new();

        private IDateTime DateTime { get; }
        private ITask Task { get; }

        public CallbackScheduler(IServiceProvider serviceProvider, Func<TKey, Task> callback, TimeSpan period)
        {
            DateTime = serviceProvider.GetService<IDateTime>();
            Task = serviceProvider.GetService<ITask>();

            m_callback = callback;
            m_period = period;
        }

        public void ScheduleCallback(TKey key, DateTime callTime)
        {
            lock(m_keysToCallbackTime)
            {
                int count = m_keysToCallbackTime.Count;
                m_keysToCallbackTime[key] = callTime;

                if (count == 0)
                    Pump().ConfigureAwait(true);
            }
        }

        public void CancelCallback(TKey key)
        {
            lock (m_keysToCallbackTime)
            {
                m_keysToCallbackTime.Remove(key);
            }
        }

        private async Task Pump()
        {
            List<TKey> toCall = new();
            bool keepPumping = true;
            do
            {
                await Task.Delay(m_period);

                lock (m_keysToCallbackTime)
                {
                    var now = DateTime.Now;
                    foreach (var kvp in m_keysToCallbackTime)
                        if (now >= kvp.Value)
                            toCall.Add(kvp.Key);

                    foreach (var key in toCall)
                        m_keysToCallbackTime.Remove(key);

                    if (m_keysToCallbackTime.Count == 0)
                        keepPumping = false;
                }

                foreach (var key in toCall)
                    CallCallback(key);

                toCall.Clear();
            } while (keepPumping);
        }

        private void CallCallback(TKey key)
        {
            m_callback(key).ConfigureAwait(true);
        }
    }
}
