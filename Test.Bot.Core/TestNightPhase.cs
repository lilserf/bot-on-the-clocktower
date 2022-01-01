using Bot.Api;
using Bot.Core;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestNightPhase : CoreTestBase
    {
        [Fact]
        public void PhaseNight_LooksUpTown()
        {
            BotGameService gs = new();
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            TownLookupMock.Verify(x => x.GetTownRecord(It.Is<ulong>(a => a == MockGuildId), It.Is<ulong>(b => b == MockChannelId)), Times.Once);
            VerifyContext();
        }

        [Fact(Skip="Committing fixes and refactoring for other tests first")]
        public void NightSendToCottages_UnauthorizedException_Continues()
        {
            Mock<IMember> memberMock = new();
            TownSquareMock.SetupGet(c => c.Users).Returns(new[] { memberMock.Object });
            memberMock.Setup(m => m.PlaceInAsync(It.IsAny<IChannel>())).ThrowsAsync(new UnauthorizedException());

            BotGameService gs = new();
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            VerifyContext();

            Assert.False(true);
        }
    }
}
