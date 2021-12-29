using Bot.Api;
using Bot.Core;
using Moq;
using Xunit;

namespace Test.Bot.Core
{
    public static class TestRunner
    {
        [Fact]
        public static void ConstructRunner_NoExceptions()
        {
            _ = new BotSystemRunner(new Mock<IBotSystem>().Object);
        }

        [Fact]
        public static void GiveRunnerSystem_CallsInitialize()
        {
            Mock<IBotSystem> sysMock = new();
            BotSystemRunner runner = new(sysMock.Object);

            sysMock.Verify(s => s.InitializeAsync(), Times.Never);

            runner.InitializeAsync().Wait(100);

            sysMock.Verify(s => s.InitializeAsync(), Times.Once);
        }
    }
}
