using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCleanup : GameTestBase
    {
        [Fact]
        public void ActivityRecorded_CleanupScheduled()
        {
            var tc = new TownCleanup(GetServiceProvider());

            var t = tc.RecordActivityAsync(MockTownKey);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            TownKeyCallbackSchedulerMock.Verify(x => x.ScheduleCallback(It.Is<TownKey>(tk => tk == MockTownKey), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public void CleanupHappened_ActivityCleared()
        {
            var tc = new TownCleanup(GetServiceProvider());

            Assert.NotNull(TownKeyCallback);
            TownKeyCallback!(MockTownKey);

            GameActivityDatabaseMock.Verify(x => x.ClearActivityAsync(It.Is<TownKey>(t => t == MockTownKey)), Times.Once);
        }

        [Fact]
        public void CurrGame_CleanupRecorded()
        {
            MockGameInProgress();

            RunCurrentGameAssertComplete();

            TownCleanupMock.Verify(x => x.RecordActivityAsync(It.Is<TownKey>(tk => tk == MockTownKey)), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CleanupRequest_RemovesRolesAndTags(bool anyoneInTown)
        {
            MockGameInProgress();
            if (!anyoneInTown)
                TownSquareMock.SetupGet(t => t.Users).Returns(Array.Empty<IMember>()); // game is ended at 4 AM, nobody is around anymore

            BotGameplay bg = new(GetServiceProvider());
            TownCleanupMock.Raise(tc => tc.CleanupRequested += null, new TownCleanupRequestedArgs(MockTownKey));

            InteractionAuthorMock.Verify(m => m.SetDisplayName(It.Is<string>(s => s == StorytellerDisplayName)), Times.Once);
            InteractionAuthorMock.Verify(m => m.RevokeRoleAsync(It.Is<IRole>(r => r == StorytellerRoleMock.Object)), Times.Once);
            Villager1Mock.Verify(m => m.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager2Mock.Verify(m => m.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager3Mock.Verify(m => m.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
        }
    }
}
