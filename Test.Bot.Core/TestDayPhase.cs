using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public class TestDayPhase : GameTestBase
	{
		[Fact]
		public void TestDay_Completes()
		{
			BotGameplay gs = new();
			var t = gs.PhaseDayAsync(InteractionContextMock.Object);
			t.Wait(50);
			Assert.True(t.IsCompleted);
		}

		[Fact]
		public void TestVote_Completes()
		{
			BotGameplay gs = new();
			var t = gs.PhaseVoteAsync(InteractionContextMock.Object);
			t.Wait(50);
			Assert.True(t.IsCompleted);
		}
	}
}
