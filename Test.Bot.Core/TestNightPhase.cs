using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public class TestNightPhase : GameTestBase
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

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        public void NightSendToCottages_ExceptionMoving1Member_Continues(Type exceptionType)
        {
            Mock<IMember> memberMock = new();
            TownSquareMock.SetupGet(c => c.Users).Returns(new[] { memberMock.Object });

            memberMock.Setup(m => m.PlaceInAsync(It.IsAny<IChannel>())).ThrowsAsync(CreateException(exceptionType));

            BotGameService gs = new();
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            VerifyContext();
        }
    }
}
