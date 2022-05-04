using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test.Bot.Base
{
    // Modiied from https://stackoverflow.com/a/43012490/10606
    public class AsyncAutoResetEvent
    {
        private readonly LinkedList<TaskCompletionSource<bool>> m_waiters = new();

        private int m_signalCount;

        public AsyncAutoResetEvent()
            : this(0)
        {}

        public AsyncAutoResetEvent(int signalCount)
        {
            m_signalCount = signalCount;
        }

        public Task<bool> WaitOneAsync(TimeSpan timeout)
        {
            return WaitOneAsync(timeout, CancellationToken.None);
        }

        public async Task<bool> WaitOneAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> tcs;

            lock (m_waiters)
            {
                if (m_signalCount > 0)
                {
                    --m_signalCount;
                    return true;
                }
                else if (timeout == TimeSpan.Zero)
                {
                    return false;
                }
                else
                {
                    tcs = new TaskCompletionSource<bool>();
                    m_waiters.AddLast(tcs);
                }
            }

            Task winner = await Task.WhenAny(tcs.Task, Task.Delay(timeout, cancellationToken));
            if (winner == tcs.Task)
            {
                // The task was signaled.
                return true;
            }
            else
            {
                // We timed-out; remove our reference to the task.
                // This is an O(n) operation since waiters is a LinkedList<T>.
                lock (m_waiters)
                {
                    bool removed = m_waiters.Remove(tcs);
                    Assert.True(removed);
                    return false;
                }
            }
        }

        public void Set()
        {
            lock (m_waiters)
            {
                if (m_waiters.Count > 0)
                {
                    // Signal the first task in the waiters list. This must be done on a new
                    // thread to avoid stack-dives and situations where we try to complete the
                    // same result multiple times.
                    TaskCompletionSource<bool> tcs = m_waiters.First!.Value;
                    Task.Run(() => tcs.SetResult(true));
                    m_waiters.RemoveFirst();
                }
                else if (m_signalCount == 0)
                {
                    // No tasks are pending
                    ++m_signalCount;
                }
            }
        }

        public override string ToString()
        {
            return $"Signal Count: {m_signalCount}, Waiters: {m_waiters.Count}";
        }
    }
}
