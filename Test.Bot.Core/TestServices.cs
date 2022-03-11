using Bot.Api;
using Bot.Base;
using Bot.Core;
using Bot.Core.Callbacks;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestServices : TestBase
    {
        [Fact]
        public static void CreateCoreServices_ConstructsType()
        {
            var sp = ServiceFactory.RegisterCoreServices(null);
            Assert.IsType<ServiceProvider>(sp);
        }

        [Fact]
        public static void CreateBotServices_NothingRegistered_Throws()
        {
            Assert.Throws<ServiceNotFoundException>(() => ServiceFactory.RegisterBotServices(new ServiceProvider()));
        }

        [Theory]
        [InlineData(typeof(ICallbackSchedulerFactory), typeof(CallbackSchedulerFactory))]
        [InlineData(typeof(IActiveGameService), typeof(ActiveGameService))]
        [InlineData(typeof(IComponentService), typeof(ComponentService))]
        [InlineData(typeof(IShuffleService), typeof(ShuffleService))]
        public void RegisterCoreServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var newSp = ServiceFactory.RegisterCoreServices(GetServiceProvider());
            var service = newSp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }

        [Theory]
        [InlineData(typeof(ITownCleanup), typeof(TownCleanup))]
        [InlineData(typeof(IVoteHandler), typeof(BotGameplay))]
        [InlineData(typeof(IBotGameplayInteractionHandler), typeof(BotGameplayInteractionHandler))]
        [InlineData(typeof(IBotMessaging), typeof(BotMessaging))]
        public void RegisterBotServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            RegisterMock(new Mock<IBotSystem>());
            RegisterMock(new Mock<ICallbackSchedulerFactory>());
            RegisterMock(new Mock<IComponentService>());

            var newSp = ServiceFactory.RegisterBotServices(GetServiceProvider());
            var service = newSp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }
    }
}
