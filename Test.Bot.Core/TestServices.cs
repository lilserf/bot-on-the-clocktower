﻿using Bot.Api;
using Bot.Api.Database;
using Bot.Base;
using Bot.Core;
using Bot.Core.Callbacks;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Threading;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestServices : TestBase
    {
        [Fact]
        public void CreateCoreServices_ConstructsType()
        {
            RegisterMock(new Mock<ILookupRoleDatabase>());
            var sp = ServiceFactory.RegisterCoreServices(GetServiceProvider(), CancellationToken.None);
            Assert.IsType<ServiceProvider>(sp);
        }

        [Fact]
        public static void CreateBotServices_NothingRegistered_Throws()
        {
            Assert.Throws<ServiceNotFoundException>(() => ServiceFactory.RegisterBotServices(new ServiceProvider()));
        }

        [Theory]
        [InlineData(typeof(IGuildInteractionErrorHandler), typeof(GuildInteractionErrorHandler))]
        [InlineData(typeof(ITownInteractionErrorHandler), typeof(TownInteractionErrorHandler))]
        [InlineData(typeof(ICallbackSchedulerFactory), typeof(CallbackSchedulerFactory))]
        [InlineData(typeof(IProcessLoggerFactory), typeof(ProcessLoggerFactory))]
        [InlineData(typeof(IComponentService), typeof(ComponentService))]
        [InlineData(typeof(IShuffleService), typeof(ShuffleService))]
        [InlineData(typeof(IFinalShutdownService), typeof(ShutdownService))]
        [InlineData(typeof(IShutdownPreventionService), typeof(ShutdownService))]
        public void RegisterCoreServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var newSp = ServiceFactory.RegisterCoreServices(GetServiceProvider(), CancellationToken.None);
            var service = newSp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }

        [Theory]
        [InlineData(typeof(IVoteHandler), typeof(BotGameplay))]
        [InlineData(typeof(IBotGameplayInteractionHandler), typeof(BotGameplayInteractionHandler))]
        [InlineData(typeof(IBotMessaging), typeof(BotMessaging))]
        [InlineData(typeof(IGuildInteractionQueue), typeof(GuildInteractionQueue))]		
        [InlineData(typeof(ITownInteractionQueue), typeof(TownInteractionQueue))]
        [InlineData(typeof(IGuildInteractionWrapper), typeof(GuildInteractionWrapper))]
        [InlineData(typeof(ITownInteractionWrapper), typeof(TownInteractionWrapper))]
        [InlineData(typeof(ITownCleanup), typeof(TownCleanup))]
        [InlineData(typeof(ITownResolver), typeof(TownResolver))]
        [InlineData(typeof(ILegacyCommandReminder), typeof(LegacyCommandReminder))]
        public void CreateBotServices_CreatesAllRequiredServices(Type serviceInterfaceType, Type serviceImplType)
        {
            RegisterMock(new Mock<IBotSystem>());
            RegisterMock(new Mock<ICallbackSchedulerFactory>());
            RegisterMock(new Mock<IShutdownPreventionService>());
            RegisterMock(new Mock<IComponentService>());

            var newSp = ServiceFactory.RegisterBotServices(GetServiceProvider());
            var impl = newSp.GetService(serviceInterfaceType);

            Assert.IsType(serviceImplType, impl);
        }
    }
}
