using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core.Callbacks
{
    public class CallbackScheduler<TKey> : ICallbackScheduler<TKey> where TKey : notnull
    {
        private readonly Func<TKey, Task> m_callback;
        private readonly TimeSpan m_period;

        private readonly Dictionary<TKey, DateTime> m_keysToCallbackTime = new();

        private readonly IDateTime DateTime;
        private readonly ITask Task;

        public CallbackScheduler(IServiceProvider serviceProvider, Func<TKey, Task> callback, TimeSpan period)
        {
            serviceProvider.Inject(out DateTime);
            serviceProvider.Inject(out Task);

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
            CallCallbackAsync(key).ConfigureAwait(true);
        }

        private async Task CallCallbackAsync(TKey key)
        {
            try
            {
                await m_callback(key);
            }
            catch (Exception)
            {
                // Not a whole lot we can do about this, as we don't have anyone to report it to at present
            }
        }
    }

    public class CallbackScheduler : ICallbackScheduler
    {
        private readonly Func<Task> m_callback;
        private readonly TimeSpan m_period;

        private readonly object m_lock = new();
        private DateTime? m_callbackTime;

        private readonly IDateTime m_dateTime;
        private readonly ITask Task;

        public CallbackScheduler(IServiceProvider serviceProvider, Func<Task> callback, TimeSpan period)
        {
            serviceProvider.Inject(out m_dateTime);
            serviceProvider.Inject(out Task);

            m_callback = callback;
            m_period = period;
        }

        public void ScheduleCallback(DateTime callTime)
        {
            lock (m_lock)
            {
                bool wasPumping = m_callbackTime.HasValue;
                m_callbackTime = callTime;
                if (!wasPumping)
                    Pump().ConfigureAwait(true);
            }
        }

        public void CancelCallback()
        {
            lock (m_lock)
            {
                m_callbackTime = null;
            }
        }

        private async Task Pump()
        {
            bool keepPumping = true;
            do
            {
                await Task.Delay(m_period);

                bool doCall = false;
                lock (m_lock)
                {
                    if (m_callbackTime.HasValue)
                    {
                        var now = m_dateTime.Now;
                        if (now >= m_callbackTime)
                        {
                            doCall = true;
                            m_callbackTime = null;
                            keepPumping = false;
                        }
                    }
                }

                if (doCall)
                    CallCallback();

            } while (keepPumping);
        }

        private void CallCallback()
        {
            CallCallbackAsync().ConfigureAwait(true);
        }

        private async Task CallCallbackAsync()
        {
            try
            {
                await m_callback();
            }
            catch (Exception)
            {
                // Not a whole lot we can do about this, as we don't have anyone to report it to at present
            }
        }
    }
}
