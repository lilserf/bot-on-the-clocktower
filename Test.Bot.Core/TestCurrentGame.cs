using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCurrentGame : GameTestBase
    {

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        [InlineData(typeof(BadRequestException))]
        [InlineData(typeof(ServerErrorException))]
        public void CurrentGame_Exceptions(Type exceptionType)
        {
            Villager1Mock.Setup(v => v.GrantRoleAsync(It.IsAny<IRole>())).ThrowsAsync(CreateException(exceptionType));

            RunCurrentGameAssertComplete();
        }

        [Fact(Skip = "Needs implementaton")]
        public void CurrentGame_NullTownSquare_ErrorMessage()
        {
            const string townSquareName = "Mock Town Square";
            TownMock.SetupGet(t => t.TownSquare).Returns((IChannel?)null);
            TownRecordMock.SetupGet(t => t.TownSquare).Returns(townSquareName);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message indicating the name of the Town Square, and some suggestions
            // for what to do about it
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains(townSquareName) && s.Contains("/createTown") && s.Contains("/addTown"))), Times.Once);
        }

        [Fact(Skip = "Needs implementaton")]
        public void CurrentGame_NullTownSquare_NullTownSquareName_ErrorMessage()
        {
            TownMock.SetupGet(t => t.TownSquare).Returns((IChannel?)null);
            TownRecordMock.SetupGet(t => t.TownSquare).Returns((string)null);

            RunCurrentGameAssertComplete();

            // Should have some sort of nice error message saying we couldn't find a Town Square, and some suggestions
            // for what to do about it
            ProcessLoggerMock.Verify(pl => pl.LogMessage(It.Is<string>(s => s.Contains("Town Square") && s.Contains("/createTown") && s.Contains("/addTown"))), Times.Once);
        }

        private void RunCurrentGameAssertComplete()
        {
            BotGameplay gs = new(GetServiceProvider());
            var t = gs.CurrentGameAsync(InteractionContextMock.Object, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }
    }
}
