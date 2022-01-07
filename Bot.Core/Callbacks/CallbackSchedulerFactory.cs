using System;
using System.Threading.Tasks;

namespace Bot.Core.Callbacks
{
    public class CallbackSchedulerFactory : ICallbackSchedulerFactory
    {
        private readonly IServiceProvider m_serviceProvider;

        public CallbackSchedulerFactory(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
        }

        public ICallbackScheduler<TKey> CreateScheduler<TKey>(Func<TKey, Task> callback, TimeSpan checkPeriod) where TKey : notnull
        {
            return new CallbackScheduler<TKey>(m_serviceProvider, callback, checkPeriod);
        }
    }
}
