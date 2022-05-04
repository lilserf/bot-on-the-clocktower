using System;

namespace Bot.Core.Callbacks
{
    public interface ICallbackScheduler<TKey> where TKey : notnull
    {
        void ScheduleCallback(TKey key, DateTime callTime);
        public void CancelCallback(TKey key);
    }
}
