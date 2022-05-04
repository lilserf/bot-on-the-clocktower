using System;
using System.Threading.Tasks;

namespace Bot.Core.Callbacks
{
    public interface ICallbackSchedulerFactory
    {
        ICallbackScheduler<TKey> CreateScheduler<TKey>(Func<TKey, Task> callback, TimeSpan checkPeriod) where TKey : notnull;
    }
}
