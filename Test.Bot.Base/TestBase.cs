using Moq;
using System;

namespace Test.Bot.Base
{
    public class TestBase
    {
        private readonly MockServiceProvider m_mockServiceProvider = new();
        protected IServiceProvider GetServiceProvider() => m_mockServiceProvider;

        protected T RegisterService<T>(T service) where T : class
        {
            m_mockServiceProvider.RegisterService(service);
            return service;
        }

        protected Mock<T> RegisterMock<T>() where T : class => RegisterMock(new Mock<T>());

        protected Mock<T> RegisterMock<T>(Mock<T> mock) where T : class
        {
            m_mockServiceProvider.RegisterService(mock.Object);
            return mock;
        }
    }
}
