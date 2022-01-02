using Bot.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Test.Bot.Core
{
	class TestDayPhase : GameTestBase
	{
		public void TestDay()
		{
			BotGameplay gs = new();
			var t = gs.PhaseDayAsync(InteractionContextMock.Object);
			t.Wait(50);
			Assert.True(t.IsCompleted);
		}
	}
}
