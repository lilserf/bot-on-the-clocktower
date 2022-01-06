using System;

namespace Bot.Core.Callbacks
{
    public interface ICallbackScheduler<TKey>
    {
        void ScheduleCallback(TKey key, DateTime callTime);
        public void CancelCallback(TKey key);
    }
}
