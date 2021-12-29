using Moq;
using System;

namespace Test.Bot.Base
{
    public class TestBase
    {
        private readonly MockServiceProvider mMockServiceProvider = new();
        protected IServiceProvider GetServiceProvider() => mMockServiceProvider;

        protected T RegisterService<T>(T service) where T : class
        {
            mMockServiceProvider.RegisterService(service);
            return service;
        }

        protected Mock<T> RegisterMock<T>(Mock<T> mock) where T : class
        {
            mMockServiceProvider.RegisterService(mock.Object);
            return mock;
        }
    }
}
