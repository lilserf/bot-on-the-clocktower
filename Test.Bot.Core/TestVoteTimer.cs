using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Test.Bot.Core
{
    public class TestVoteTimer : GameTestBase
    {
        private static readonly DateTime s_defaultDateTimeNow = new(2021, 2, 3, 4, 5, 6, 7);
        private readonly Mock<IDateTime> MockDateTime = new();
        private DateTime m_currentTime = s_defaultDateTimeNow;

        public TestVoteTimer()
            : base()
        {
            RegisterMock(MockDateTime);
            MockDateTime.SetupGet(dt => dt.Now).Returns(() => m_currentTime);
        }


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
        
        [Fact]
        public void RunValidTimer_SendsChatMessagesAppropriately()
        {
            var voteHandlerMock = RegisterMock(new Mock<IVoteHandler>());

            // NOTE: I could have done a lot more testing for this, but the entire happy path after validating the town, chat channel, villager role,
            // and time requested was ported from entirely working python code, so I am just doing that path
            Func<ITownRecord, Task>? callback = null;
            CallbackSchedulerFactoryMock
                .Setup(csf => csf.CreateScheduler(It.IsAny<Func<ITownRecord, Task>>(), It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))))
                .Callback<Func<ITownRecord, Task>, TimeSpan>((f, ts) => callback = f)
                .Returns(TownRecordCallbackSchedulerMock.Object);

            TownRecordCallbackSchedulerMock.Setup(cs => cs.ScheduleCallback(It.IsAny<ITownRecord>(), It.IsAny<DateTime>()));

            RunVoteTimerVerifyCompleted("6 minutes");
            
            Assert.NotNull(callback);
            voteHandlerMock.Verify(vh => vh.PerformVoteAsync(It.IsAny<ITownRecord>()), Times.Never);
            TownRecordCallbackSchedulerMock.Verify(cs => cs.ScheduleCallback(It.Is<ITownRecord>(tr => tr == TownRecordMock.Object), It.IsAny<DateTime>()), Times.Once);
            ChatChannelMock.Verify(c => c.SendMessageAsync(It.Is<string>(s => s.Contains(VillagerRoleMock.Object.Mention) && s.Contains("6 minutes") && s.Contains("vote", StringComparison.InvariantCultureIgnoreCase))), Times.Once);

            AdvanceTime(TimeSpan.FromMinutes(6));

            callback!(TownRecordMock.Object);

            voteHandlerMock.Verify(vh => vh.PerformVoteAsync(It.Is<ITownRecord>(tr => tr == TownRecordMock.Object)), Times.Once);
            ChatChannelMock.Verify(c => c.SendMessageAsync(It.Is<string>(s => s.Contains(VillagerRoleMock.Object.Mention) && s.Contains(TownSquareMock.Object.Name) && s.Contains("returning", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
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

        private void AdvanceTime(TimeSpan span)
        {
            m_currentTime += span;
        }
    }
}
