using System;
using System.Collections.Generic;

namespace Bot.Base
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> m_services = new();

        private readonly IServiceProvider? m_parent = null;

        public ServiceProvider()
            : this(null)
        {}

        public ServiceProvider(IServiceProvider parent)
        {
            m_parent = parent;
        }

        public object? GetService(Type serviceType)
        {
            if (m_services.TryGetValue(serviceType, out var ret))
                return ret;
            return m_parent?.GetService(serviceType);
        }

        public void AddService<T>(T service) where T : class
        {
            if (m_services.ContainsKey(typeof(T))) throw new ServiceAlreadyAddedException(typeof(T));
            m_services.Add(typeof(T), service);
        }

        public class ServiceAlreadyAddedException : Exception
        {
            public Type TypeAlreadyAdded { get; }

            public ServiceAlreadyAddedException(Type t)
            {
                TypeAlreadyAdded = t;
            }
        }
    }
}
