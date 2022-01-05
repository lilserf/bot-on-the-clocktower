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

            // Interaction Author, as storyteller, should move first
            InteractionAuthorMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                iaCalls++;
                iaCheck = (v2Calls == 0) && (v1Calls == 0) && (iaCalls == 1);
            });
            // Villager1 is Bob and would normally move last but our mock Shuffle makes him next
            Villager1Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                v1Calls++;
                v1Check = (v2Calls == 0) && (v1Calls == 1) && (iaCalls == 1);
            });
            // Then Villager2 who is Alice ends up last
            Villager2Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Callback(() =>
            {
                v2Calls++;
                v2Check = (v2Calls == 1) && (v1Calls == 1) && (iaCalls == 1);
            });

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
            Assert.True(iaCheck, "InteractionAuthor should be moved first as storyteller");
            Assert.True(v1Check, "Villager1 should be moved second");
            Assert.True(v2Check, "Villager2 should be moved last");
        }

        // Test that storyteller got the Storyteller role
        // Test that villagers got the villager role
        // TODO: move this to another module since it's not strictly Night
        // TODO: more complex setup where some users already have the roles and shouldn't get GrantRole called
        // TODO: old players should lose the roles?
        [Fact]
        public void CurrentGame_RolesCorrect()
		{
            BotGameplay gs = new(GetServiceProvider());
            var t = gs.CurrentGameAsync(InteractionContextMock.Object, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            InteractionAuthorMock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == StoryTellerRoleMock.Object)), Times.Once);
            Villager1Mock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager2Mock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
		}

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        [InlineData(typeof(BadRequestException))]
        [InlineData(typeof(ServerErrorException))]
        public void CurrentGame_Exceptions(Type exceptionType)
		{
            Villager1Mock.Setup(v => v.GrantRoleAsync(It.IsAny<IRole>())).ThrowsAsync(CreateException(exceptionType));

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.CurrentGameAsync(InteractionContextMock.Object, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
		}
    }
}
