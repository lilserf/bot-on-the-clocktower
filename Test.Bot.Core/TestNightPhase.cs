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
            BotGameplay gs = new();
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

            BotGameplay gs = new();
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            VerifyContext();
        }

        [Fact]
        public void Night_CottagesCorrect()
		{
            BotGameplay gs = new();
            var t = gs.PhaseNightAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Storyteller should go in Cottage1 since they're the Storyteller
            // Alice (Villager2) should go in Cottage2
            // Bot (Villager1) should go in Cottage3

            InteractionAuthorMock.Verify(v => v.PlaceInAsync(It.Is<IChannel>(c => c == Cottage1Mock.Object)), Times.Once);
            Villager2Mock.Verify(v => v.PlaceInAsync(It.Is<IChannel>(c => c == Cottage2Mock.Object)), Times.Once);
            Villager1Mock.Verify(v => v.PlaceInAsync(It.Is<IChannel>(c => c == Cottage3Mock.Object)), Times.Once);

            VerifyContext();
        }

        // Test that storyteller got the Storyteller role
        // Test that villagers got the villager role
        // TODO: move this to another module since it's not strictly Night
        // TODO: more complex setup where some users already have the roles and shouldn't get GrantRole called
        // TODO: old players should lose the roles?
        [Fact]
        public void CurrentGame_RolesCorrect()
		{
            BotGameplay gs = new();
            var t = gs.CurrentGameAsync(InteractionContextMock.Object, TownMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            InteractionAuthorMock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == StoryTellerRoleMock.Object), It.IsAny<string>()), Times.Once);
            Villager1Mock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object), It.IsAny<string>()), Times.Once);
            Villager2Mock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object), It.IsAny<string>()), Times.Once);
		}
    }
}
