using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public static class TestServices
    {
        [Fact]
        public static void ServiceProvider_Constructs_NoExceptions()
        {
            var _ = new ServiceProvider();
        }

        [Fact]
        public static void ServiceProvider_NoService_ReturnsNull()
        {
            ServiceProvider sp = new();
            object? obj = sp.GetService(typeof(IEnvironment));
            Assert.Null(obj);
        }

        [Fact]
        public static void ServiceProviderExtension_NoService_ThrowsException()
        {
            ServiceProvider sp = new();
            Assert.Throws<ServiceNotFoundException>(() => sp.GetService<IEnvironment>());
        }

        [Fact]
        public static void ServiceProvider_AddService_ReturnsIt()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> mockEnv = new();

            sp.AddService(mockEnv.Object);

            var ret = sp.GetService<IEnvironment>();
            Assert.Equal(mockEnv.Object, ret);
        }

        [Fact]
        public static void ServiceProvider_AddServiceTwice_ThrowsException()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> mockEnv1 = new();
            Mock<IEnvironment> mockEnv2 = new();

            sp.AddService(mockEnv1.Object);
            Assert.Throws<ServiceProvider.ServiceAlreadyAddedException>(() => sp.AddService(mockEnv2.Object));
        }

        [Fact]
        public static void CreateServices_ConstructsType()
        {
            var sp = ServiceProviderFactory.CreateServiceProvider();
            Assert.IsType<ServiceProvider>(sp);
        }

        [Fact]
        public static void CreateServices_HasEnvironment()
        {
            var sp = ServiceProviderFactory.CreateServiceProvider();
            var env = sp.GetService<IEnvironment>();

            Assert.IsType<BotEnvironment>(env);
        }
    }
}
