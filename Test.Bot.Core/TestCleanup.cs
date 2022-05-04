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
        public void CurrGame_Cleanup(bool gameInProgress)
        {
            if(gameInProgress)
                MockGameInProgress();
            
            BotGameplay gs = new(GetServiceProvider());
            var t = gs.CurrentGameAsync(MockTownKey, InteractionAuthorMock.Object, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            TownCleanupMock.Raise(tc => tc.CleanupRequested += null, new TownCleanupRequestedArgs(MockTownKey));

            Villager1Mock.Verify(c => c.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager2Mock.Verify(c => c.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager3Mock.Verify(c => c.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
        }
    }
}
