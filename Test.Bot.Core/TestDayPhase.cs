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
			var gs = CreateGameplayInteractionHandler();
			var t = gs.CommandDayAsync(InteractionContextMock.Object);
			t.Wait(50);
			Assert.True(t.IsCompleted);
		}

		[Fact]
		public void TestVote_Completes()
		{
			var gs = CreateGameplayInteractionHandler();
			var t = gs.CommandVoteAsync(InteractionContextMock.Object);
			t.Wait(50);
			Assert.True(t.IsCompleted);
		}
	}
}
