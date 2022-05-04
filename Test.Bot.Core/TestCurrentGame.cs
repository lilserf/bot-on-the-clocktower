using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCurrentGame : GameTestBase
    {
        // Test that storyteller got the Storyteller role
        // Test that villagers got the villager role
        // TODO: more complex setup where some users already have the roles and shouldn't get GrantRole called
        // TODO: old players should lose the roles?
        [Fact]
        public void CurrentGame_RolesCorrect()
        {
            RunCurrentGameAssertComplete();

            InteractionAuthorMock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == StorytellerRoleMock.Object)), Times.Once);
            Villager1Mock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
            Villager2Mock.Verify(x => x.GrantRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
        }

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        [InlineData(typeof(ServerErrorException))]
        public void CurrentGame_GrantRole_Exceptions(Type exceptionType)
        {
            Villager1Mock.Setup(v => v.GrantRoleAsync(It.IsAny<IRole>())).ThrowsAsync(CreateException(exceptionType));

            RunCurrentGameAssertComplete();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullTownSquare_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            const string townSquareName = "Mock Town Square";
            TownMock.SetupGet(t => t.TownSquare).Returns((IChannel?)null);
            TownRecordMock.SetupGet(t => t.TownSquare).Returns(townSquareName);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message indicating the name of the Town Square
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains(townSquareName))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullTownSquare_NullTownSquareName_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            TownMock.SetupGet(t => t.TownSquare).Returns((IChannel?)null);
            TownRecordMock.SetupGet(t => t.TownSquare).Returns((string?)null);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message saying we couldn't find a Town Square, and some suggestions for what to do about it
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("Town Square", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/createTown", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/addTown", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullDayCategory_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            const string dayCategoryName = "Mock Murdertown";
            TownMock.SetupGet(t => t.DayCategory).Returns((IChannelCategory?)null);
            TownRecordMock.SetupGet(t => t.DayCategory).Returns(dayCategoryName);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message indicating the name of the Day Category
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains(dayCategoryName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullDayCategory_NullDayCategoryName_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            TownMock.SetupGet(t => t.DayCategory).Returns((IChannelCategory?)null);
            TownRecordMock.SetupGet(t => t.DayCategory).Returns((string?)null);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message saying we couldn't find the primary category, and some suggestions for what to do about it
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("category", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/createTown", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/addTown", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullNightCategory_Continues(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            const string nightCategoryName = "Mock Murdertown - Night";
            TownMock.SetupGet(t => t.NightCategory).Returns((IChannelCategory?)null);
            TownRecordMock.SetupGet(t => t.NightCategory).Returns(nightCategoryName);

            RunCurrentGameAssertComplete();

            // No error messages here - it is valid to not have a night category
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullStorytellerRole_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            const string storytellerRoleName = "Mock Storyteller Role";
            TownMock.SetupGet(t => t.StorytellerRole).Returns((IRole?)null);
            TownRecordMock.SetupGet(t => t.StorytellerRole).Returns(storytellerRoleName);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message indicating the role name
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains(storytellerRoleName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullStorytellerRole_NullStorytellerRoleName_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            TownMock.SetupGet(t => t.StorytellerRole).Returns((IRole?)null);
            TownRecordMock.SetupGet(t => t.StorytellerRole).Returns((string?)null);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message saying we couldn't find the role, and some suggestions for what to do about it
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("storyteller", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/createTown", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/addTown", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullVillagerRole_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            const string villagerRoleName = "Mock Villager Role";
            TownMock.SetupGet(t => t.VillagerRole).Returns((IRole?)null);
            TownRecordMock.SetupGet(t => t.VillagerRole).Returns(villagerRoleName);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message indicating the role name
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains(villagerRoleName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NullVillagerRole_NullVillagerRoleName_ErrorMessage(bool gameInProgress)
        {
            if (gameInProgress)
                MockGameInProgress();

            TownMock.SetupGet(t => t.VillagerRole).Returns((IRole?)null);
            TownRecordMock.SetupGet(t => t.VillagerRole).Returns((string?)null);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message saying we couldn't find the role, and some suggestions for what to do about it
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("villager", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/createTown", StringComparison.InvariantCultureIgnoreCase) && s.Contains("/addTown", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_NewStoryteller_Tag(bool gameInProgress)
        {
            if(gameInProgress)
            {
                MockGameInProgress();
            }
            var stName = "Carol";
            InteractionAuthorMock.SetupGet(m => m.DisplayName).Returns(stName);

            RunCurrentGameAssertComplete();

            var newName = "(ST) " + stName;
            InteractionAuthorMock.Verify(m => m.SetDisplayName(newName), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_StorytellerAlreadyTagged(bool gameInProgress)
        {
            if(gameInProgress)
            {
                MockGameInProgress();
            }
            var stName = "(ST) Carol";
            InteractionAuthorMock.SetupGet(m => m.DisplayName).Returns(stName);

            RunCurrentGameAssertComplete();

            InteractionAuthorMock.Verify(m => m.SetDisplayName(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentGame_UnTagVillagers(bool gameInProgress)
        {
            if (gameInProgress)
            {
                MockGameInProgress();
            }
            var v1Name = "(ST) Dave";
            Villager1Mock.SetupGet(m => m.DisplayName).Returns(v1Name);

            RunCurrentGameAssertComplete();

            // Dave should get the tag removed
            Villager1Mock.Verify(m => m.SetDisplayName("Dave"), Times.Once);
            // Other villager shouldn't be messed with
            Villager2Mock.Verify(m => m.SetDisplayName(It.IsAny<string>()), Times.Never);
        }



        [Theory]
        [InlineData(typeof(NotFoundException))]
        [InlineData(typeof(ServerErrorException))]
        public void CurrentGame_StorytellerTag_Exceptions(Type exceptionType)
        {
            // We no longer test for this to throw an UnauthorizedException because that path catches and continues
            InteractionAuthorMock.Setup(m => m.SetDisplayName(It.IsAny<string>())).ThrowsAsync(CreateException(exceptionType));

            RunCurrentGameAssertComplete();
            ProcessLoggerMock.Verify(pl => pl.LogException(It.Is<Exception>(s => s.GetType() == exceptionType), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void CurrentGame_StorytellerSwitch()
        {
            MockGameInProgress();

            // Make Villager1 send the message, not IAuthor
            var game = RunCurrentGameAssertComplete(Villager1Mock.Object);

            Assert.NotNull(game);
            Assert.Contains(Villager1Mock.Object, game!.Storytellers);
            Assert.DoesNotContain(InteractionAuthorMock.Object, game!.Storytellers);
            Assert.Contains(InteractionAuthorMock.Object, game!.Villagers);
            Assert.DoesNotContain(Villager1Mock.Object, game!.Villagers);
        }

        [Fact]
        public void CurrentGame_NewPeople()
        {
            TownSquareMock.SetupGet(c => c.Users).Returns(new[] { InteractionAuthorMock.Object, Villager1Mock.Object, Villager2Mock.Object });
            RunCurrentGameAssertComplete();

            TownSquareMock.SetupGet(c => c.Users).Returns(new[] { InteractionAuthorMock.Object, Villager1Mock.Object, Villager2Mock.Object, Villager3Mock.Object });
            RunCurrentGameAssertComplete();

            Villager3Mock.Verify(m => m.GrantRoleAsync(It.Is<IRole>(r => r == VillagerRoleMock.Object)), Times.Once);
        }

        [Fact]
        public void CurrentGame_NotEnoughPlayers()
        {
            TownSquareMock.SetupGet(c => c.Users).Returns(Enumerable.Empty<IMember>().ToList());

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.CurrentGameAsync(MockTownKey, InteractionAuthorMock.Object, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            IGame? result = t.Result;
            Assert.Null(result);
        }

    }
}
