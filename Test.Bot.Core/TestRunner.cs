using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestRunner : TestBase
    {
        [Fact]
        public void ConstructRunner_NoExceptions()
        {
            _ = new BotSystemRunner(GetServiceProvider(), new Mock<IBotSystem>().Object);
        }

        [Fact]
        public void GiveRunnerSystem_CreatesClient()
        {
            Mock<IBotSystem> sysMock = new();
            BotSystemRunner runner = new(GetServiceProvider(), sysMock.Object);

            sysMock.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Never);

            runner.RunAsync().Wait(100);

            sysMock.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Once);
        }
    }
}
