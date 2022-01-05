using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public class TestNightPhase : GameTestBase
    {
        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        public void NightSendToCottages_ExceptionMoving1Member_Continues(Type exceptionType)
        {
            Mock<IMember> memberMock = new();
            TownSquareMock.SetupGet(c => c.Users).Returns(new[] { memberMock.Object });

            memberMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).ThrowsAsync(CreateException(exceptionType));

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            VerifyContext();
        }

        [Fact]
        public void Night_CottagesCorrect()
		{
            BotGameplay gs = new(GetServiceProvider());
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Storyteller should go in Cottage1 since they're the Storyteller
            // Alice (Villager2) should go in Cottage2
            // Bot (Villager1) should go in Cottage3

            InteractionAuthorMock.Verify(v => v.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage1Mock.Object)), Times.Once);
            Villager2Mock.Verify(v => v.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage2Mock.Object)), Times.Once);
            Villager1Mock.Verify(v => v.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage3Mock.Object)), Times.Once);

            VerifyContext();
        }

        [Fact]
        public void Night_MoveOrder()
        {
            int v1Calls = 0;
            int v2Calls = 0;
            int iaCalls = 0;

            bool v1Check = true;
            bool v2Check = true;
            bool iaCheck = true;

            // First Villager 2 should get moved
            Villager2Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                v2Calls++;
                v2Check = (v2Calls == 1) && (v1Calls == 0) && (iaCalls == 0);
            });
            // Next Villager 1 should get moved
            Villager1Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                v1Calls++;
                v1Check = (v2Calls == 1) && (v1Calls == 1) && (iaCalls == 0);
            });
            // Finally the Interaction Author
            InteractionAuthorMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                iaCalls++;
                iaCheck = (v2Calls == 1) && (v1Calls == 1) && (iaCalls == 1);
            });

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
            Assert.True(v1Check);
            Assert.True(v2Check);
            Assert.True(iaCheck);
        }
    }
}
