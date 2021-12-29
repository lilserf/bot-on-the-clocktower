using Moq;
using System;
using System.Collections.Generic;

namespace Test.Bot.Base
{
    public class MockServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> mMapping = new();

        public object? GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (!mMapping.TryGetValue(serviceType, out var ret))
            {
                // No service yet, create a strict-behaving mock as a placeholder
                var mockType = typeof(Mock<>).MakeGenericType(serviceType);
                var constr = mockType.GetConstructor(new[] { typeof(MockBehavior) });
                Mock m = (Mock)constr!.Invoke(new object?[] { MockBehavior.Strict });
                ret = m.Object;
                mMapping.Add(serviceType, ret);
            }
            return ret;
        }

        public void RegisterService<T>(T service) where T : class
        {
            mMapping[typeof(T)] = service;
        }
    }
}
