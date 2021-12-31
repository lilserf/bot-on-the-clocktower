using Bot.Api;
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

        protected const ulong MockGuildId = 1337;
        protected const ulong MockChannelId = 42;

        // Get a standard InteractionContext with some useful mocks within it
        protected Mock<IBotInteractionContext> GetStandardContext()
		{
            Mock<IGuild> guildMock = new();
            Mock<IChannel> channelMock = new();
            guildMock.SetupGet(x => x.Id).Returns(MockGuildId);
            channelMock.SetupGet(x => x.Id).Returns(MockChannelId);

            Mock<IBotInteractionContext> contextMock = new();
            contextMock.SetupGet(c => c.Services).Returns(GetServiceProvider());
            contextMock.SetupGet(x => x.Guild).Returns(guildMock.Object);
            contextMock.SetupGet(x => x.Channel).Returns(channelMock.Object);

            return contextMock;
        }

        // Common assumptions we want to make for most of our Interactions
        protected void VerifyContext(Mock<IBotInteractionContext> contextMock)
		{
            // Most interactions should Defer to allow for latency
            contextMock.Verify(c => c.CreateDeferredResponseMessageAsync(), Times.Once);
        }
    }
}
