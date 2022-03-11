using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCleanup : GameTestBase
    {

        [Fact]
        public void CurrGame_CleanupScheduled()
        {
            MockGameInProgress();

            RunCurrentGameAssertComplete();

            GameActivityDatabaseMock.Verify(x => x.RecordActivity(It.Is<TownKey>(tk => tk == MockTownKey)), Times.Once);
            TownKeyCallbackSchedulerMock.Verify(x => x.ScheduleCallback(It.Is<TownKey>(tk => tk == MockTownKey), It.IsAny<DateTime>()), Times.Once);

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

            var c = gs.CleanupTown(MockTownKey);
            c.Wait(50);
            Assert.True(c.IsCompleted);

            if(gameInProgress)
                ActiveGameServiceMock.Verify(x => x.EndGame(It.Is<ITown>(t => t == TownMock.Object)), Times.Once);

            Villager1Mock.Verify(c => c.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager2Mock.Verify(c => c.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager3Mock.Verify(c => c.RevokeRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);

            GameActivityDatabaseMock.Verify(x => x.ClearActivity(It.Is<TownKey>(t => t == MockTownKey)), Times.Once);
        }

    }
}
