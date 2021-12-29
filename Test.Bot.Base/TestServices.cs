using Bot.Base;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Base
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
            object? obj = sp.GetService(typeof(ITestInterface));
            Assert.Null(obj);
        }

        [Fact]
        public static void ServiceProviderExtension_NoService_ThrowsException()
        {
            ServiceProvider sp = new();
            Assert.Throws<ServiceNotFoundException>(() => sp.GetService<ITestInterface>());
        }

        [Fact]
        public static void ServiceProvider_AddService_ReturnsIt()
        {
            ServiceProvider sp = new();
            Mock<ITestInterface> mock = new();

            sp.AddService(mock.Object);

            var ret = sp.GetService<ITestInterface>();
            Assert.Equal(mock.Object, ret);
        }

        [Fact]
        public static void ServiceProvider_AddServiceTwice_ThrowsException()
        {
            ServiceProvider sp = new();
            Mock<ITestInterface> mock1 = new();
            Mock<ITestInterface> mock2 = new();

            sp.AddService(mock1.Object);
            Assert.Throws<ServiceProvider.ServiceAlreadyAddedException>(() => sp.AddService(mock2.Object));
        }

        [Fact]
        public static void ServiceProvider_IsChild_ReturnsParentService()
        {
            ServiceProvider sp1 = new();
            ServiceProvider sp2 = new(sp1);
            Mock<ITestInterface> mock1 = new();
            sp1.AddService(mock1.Object);

            var fromSp2 = sp2.GetService<ITestInterface>();

            Assert.Equal(mock1.Object, fromSp2);
        }

        [Fact]
        public static void ServiceProvider_ChildOverrides_ReturnsChildService()
        {
            ServiceProvider sp1 = new();
            ServiceProvider sp2 = new(sp1);
            Mock<ITestInterface> mock1 = new();
            Mock<ITestInterface> mock2 = new();
            sp1.AddService(mock1.Object);
            sp2.AddService(mock2.Object);

            var fromSp2 = sp2.GetService<ITestInterface>();

            Assert.Equal(mock2.Object, fromSp2);
        }

        public interface ITestInterface
        { }
    }
}
