using Moq;
using System;
using Test.Bot.Core.Interaction;
using Xunit;

namespace Test.Bot.Core
{
    public class TestDayPhase : GameTestBase
	{
		[Fact]
		public void TestDay_Completes()
		{
			var gs = CreateGameplayInteractionHandler();

			AssertCompletedTask(() => gs.CommandDayAsync(InteractionContextMock.Object));
		}

		[Fact]
		public void TestVote_Completes()
		{
			var gs = CreateGameplayInteractionHandler();

			AssertCompletedTask(() => gs.CommandVoteAsync(InteractionContextMock.Object));
		}

		[Fact]
		public void Day_OutputsVerboseLogging()
		{
			var gs = CreateGameplayInteractionHandler();


			AssertCompletedTask(() => gs.CommandDayAsync(InteractionContextMock.Object));

            ProcessLoggerMock.Verify(pl => pl.LogVerbose(It.Is<string>(s => s.Contains("day", StringComparison.InvariantCultureIgnoreCase))), Times.AtLeastOnce);
		}


		[Fact]
		public void Vote_OutputsVerboseLogging()
		{
			var gs = CreateGameplayInteractionHandler();

			AssertCompletedTask(() => gs.CommandVoteAsync(InteractionContextMock.Object));

			ProcessLoggerMock.Verify(pl => pl.LogVerbose(It.Is<string>(s => s.Contains("vote", StringComparison.InvariantCultureIgnoreCase))), Times.AtLeastOnce);
		}
	}
}
