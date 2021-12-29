using System;
using System.Collections.Generic;

namespace Bot.Core
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> mServices = new();

        public object? GetService(Type serviceType)
        {
            if (mServices.TryGetValue(serviceType, out var ret))
                return ret;
            return null;
        }

        public void AddService<T>(T service) where T : class
        {
            if (mServices.ContainsKey(typeof(T))) throw new ServiceAlreadyAddedException(typeof(T));
            mServices.Add(typeof(T), service);
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
