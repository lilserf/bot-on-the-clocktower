using Bot.Api;
using Bot.Core;
using Bot.Core.Interaction;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public class TestNightPhase : GameTestBase
    {
        public TestNightPhase()
        {
            RegisterService<ITownInteractionErrorHandler>(new TownInteractionErrorHandler());
        }

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        public void NightSendToCottages_ExceptionMoving1Member_Continues(Type exceptionType)
        {
            // For some reason, this code that creates its own member will cause a straight-up access violation when
            // CurrentGameAsync tries to remove the storyteller from the list of all users (whaaaat?)
            // This doesn't happen for some reason if we use one of the user mocks the base class created
            //Mock<IMember> memberMock = new();
            //SetupUserMock(memberMock, "Ethel");
            //TownSquareMock.SetupGet(c => c.Users).Returns(new[] { memberMock.Object, InteractionAuthorMock.Object });
            //memberMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).ThrowsAsync(CreateException(exceptionType));

            Villager1Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).ThrowsAsync(CreateException(exceptionType));

            var gs = CreateGameplayInteractionHandler();
            var t = gs.PhaseNightInternal(MockTownKey, InteractionAuthorMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }

        [Fact]
        public void Night_CottagesCorrect()
		{
            var gs = CreateGameplayInteractionHandler();
            var t = gs.PhaseNightInternal(MockTownKey, InteractionAuthorMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Storyteller should go in Cottage1 since they're the Storyteller
            // Alice (Villager2) should go in Cottage2
            // Bot (Villager1) should go in Cottage3

            InteractionAuthorMock.Verify(v => v.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage1Mock.Object)), Times.Once);
            Villager2Mock.Verify(v => v.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage2Mock.Object)), Times.Once);
            Villager1Mock.Verify(v => v.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage3Mock.Object)), Times.Once);
        }

        // Test that users are moved in the order the IShuffleService dictates
        [Fact]
        public void Night_MoveOrder()
        {
            int v1Calls = 0;
            int v2Calls = 0;
            int iaCalls = 0;

            bool v1Check = true;
            bool v2Check = true;
            bool iaCheck = true;

            // Villager1 is Bob and would normally move last but our mock Shuffle makes him next
            Villager1Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                v1Calls++;
                v1Check = (v2Calls == 0) && (v1Calls == 1) && (iaCalls == 0);
            });
            // Then Villager2 who is Alice ends up last
            Villager2Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                v2Calls++;
                v2Check = (v2Calls == 1) && (v1Calls == 1) && (iaCalls == 0);
            });
            // Interaction Author, as storyteller, should move last
            InteractionAuthorMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                iaCalls++;
                iaCheck = (v2Calls == 1) && (v1Calls == 1) && (iaCalls == 1);
            });

            var gs = CreateGameplayInteractionHandler();
            var t = gs.PhaseNightInternal(MockTownKey, InteractionAuthorMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
            Assert.True(iaCheck, "InteractionAuthor should be moved first as storyteller");
            Assert.True(v1Check, "Villager1 should be moved second");
            Assert.True(v2Check, "Villager2 should be moved last");
        }

        [Fact]
        public void TwoStorytellers_BothMoveToSameCottage()
        {
            Mock<IMember> st2 = new();
            SetupUserMock(st2, "Other ST");

            TownSquareMock.SetupGet(t => t.Users).Returns(new[] { InteractionAuthorMock.Object, st2.Object, Villager1Mock.Object });

            var gameMock = CreateGameMock();
            gameMock.SetupGet(g => g.Storytellers).Returns(new[] { InteractionAuthorMock.Object, st2.Object });
            gameMock.SetupGet(g => g.Villagers).Returns(new[] { Villager1Mock.Object });

            BotGameplay g = new(GetServiceProvider());
            AssertCompletedTask(() => g.PhaseNightUnsafe(gameMock.Object, ProcessLoggerMock.Object));

            InteractionAuthorMock.Verify(m => m.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage1Mock.Object)), Times.Once);
            st2.Verify(m => m.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage1Mock.Object)), Times.Once);
            Villager1Mock.Verify(m => m.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage2Mock.Object)), Times.Once);
        }

        [Fact]
        public void StorytellersAlreadyInNightCategory_DoesNotMove()
        {
            TownSquareMock.SetupGet(t => t.Users).Returns(Array.Empty<IMember>());
            Cottage2Mock.SetupGet(x => x.Users).Returns(new [] {InteractionAuthorMock.Object });

            var gameMock = CreateGameMock();

            BotGameplay g = new(GetServiceProvider());
            AssertCompletedTask(() => g.PhaseNightUnsafe(gameMock.Object, ProcessLoggerMock.Object));

            InteractionAuthorMock.Verify(m => m.MoveToChannelAsync(It.Is<IChannel>(c => c == Cottage1Mock.Object)), Times.Never);
        }
    }
}
