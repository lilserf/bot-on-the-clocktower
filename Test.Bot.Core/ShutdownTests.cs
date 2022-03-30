using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class ShutdownTests : TestBase
    {
        private readonly Mock<IShutdownPreventionService> m_mockShutdownPrevention = new(MockBehavior.Strict);
        private readonly List<Task> m_registeredPreventerTasks = new();


        public ShutdownTests()
        {
            RegisterMock(m_mockShutdownPrevention);

            m_mockShutdownPrevention.SetupAdd(sp => sp.ShutdownRequested += (sender, args) => { });
            m_mockShutdownPrevention.Setup(sp => sp.RegisterShutdownPreventer(It.IsAny<Task>())).Callback<Task>(m_registeredPreventerTasks.Add);
        }

        [Fact]
        public void TownCommandQueue_RegistersAsPreventer()
        {
            var tcq = new TownCommandQueue(GetServiceProvider());

            m_mockShutdownPrevention.Verify(sp => sp.RegisterShutdownPreventer(It.IsAny<Task>()), Times.Once());
            Assert.Collection(m_registeredPreventerTasks,
                t => Assert.False(t.IsCompleted));
        }
    }
}
