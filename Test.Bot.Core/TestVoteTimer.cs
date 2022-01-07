using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public class TestVoteTimer : GameTestBase
    {
        [Fact]
        public void BotServices_RegistersType()
        {
            var sp = ServiceFactory.RegisterBotServices(GetServiceProvider());
            var vts = sp.GetService<IBotVoteTimer>();

            Assert.IsType<BotVoteTimer>(vts);
        }

        [Fact]
        public void RunVoteTimer_DefersContext()
        {
            var vt = new BotVoteTimer(GetServiceProvider());

            var t = vt.RunVoteTimerAsync(InteractionContextMock.Object, "");
            t.Wait(50);
            Assert.True(t.IsCompleted);

            VerifyContext();
        }

        [Fact]
        public void InvalidTimeString_SendsErrorMessage()
        {
            RunVoteTimerVerifyCompleted("invalid message");

            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("format", StringComparison.InvariantCultureIgnoreCase) && s.Contains("time", StringComparison.InvariantCultureIgnoreCase) && s.Contains("please", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public void NoTownRecordFound_SendsErrorMessage()
        {
            TownLookupMock.Setup(tl => tl.GetTownRecord(It.IsAny<ulong>(), It.IsAny<ulong>())).ReturnsAsync(() => (ITownRecord?)null);

            RunVoteTimerVerifyCompleted();

            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("/createTown", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/addTown", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public void NoTownFound_SendsErrorMessage()
        {
            ClientMock.Setup(c => c.ResolveTownAsync(It.IsAny<ITownRecord>())).ReturnsAsync(() => (ITown?)null);

            RunVoteTimerVerifyCompleted();

            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("/createTown", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/addTown", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public void NoChatChannel_SendsErrorMessage()
        {
            TownMock.SetupGet(t => t.ChatChannel).Returns((IChannel?)null);

            RunVoteTimerVerifyCompleted();

            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("/setChatChannel", StringComparison.InvariantCultureIgnoreCase) && s.Contains("no chat channel found", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public void NoVillagerRole_SendsErrorMessage()
        {
            TownMock.SetupGet(t => t.VillagerRole).Returns((IRole?)null);

            RunVoteTimerVerifyCompleted();

            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("/addTown", StringComparison.InvariantCultureIgnoreCase) && s.Contains("no villager role found", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData("5 seconds")]
        [InlineData("30 minutes")]
        public void TimeOutOfBounds_SendsErrorMessage(string timeString)
        {
            RunVoteTimerVerifyCompleted(timeString);

            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("time", StringComparison.InvariantCultureIgnoreCase) && s.Contains("between", StringComparison.InvariantCultureIgnoreCase) && s.Contains("please", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        private void RunVoteTimerVerifyCompleted()
        {
            RunVoteTimerVerifyCompleted("");
        }

        private void RunVoteTimerVerifyCompleted(string timeString)
        {
            var vt = new BotVoteTimer(GetServiceProvider());

            var t = vt.RunVoteTimerUnsafe(InteractionContextMock.Object, timeString, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }
    }
}
