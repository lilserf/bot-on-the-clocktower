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
    public class TestMessaging : GameTestBase
    {
        [Fact]
        public void TestEvilMessage()
        {
            BotMessaging bm = new(GetServiceProvider());

            var t = bm.SendEvilMessage(Villager1Mock.Object, new[] { Villager2Mock.Object, Villager3Mock.Object });
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Villager 1 should hear they're the demon, and see their minion's names mentioned
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are the **demon**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager2Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager3Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            // Villager 2 should hear they're a minion, and see the demon and their fellow minion's names
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are a **minion**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager1Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager3Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            // Villager 3 should hear they're a minion, and see the demon and their fellow minion's names
            Villager3Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are a **minion**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager3Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager1Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager3Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager2Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public void TestLunaticMessage()
        {
            BotMessaging bm = new(GetServiceProvider());

            var t = bm.SendLunaticMessage(Villager1Mock.Object, new[] { Villager2Mock.Object, Villager3Mock.Object });
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Villager 1 should hear they're the demon, and see their minion's names mentioned
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are the **demon**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager2Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager3Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            // Villager 2 should NOT hear anything because this is all fake
            Villager2Mock.Verify(x => x.SendMessageAsync(It.IsAny<string>()), Times.Never);
            // Villager 3 should NOT hear anything because this is all fake
            Villager3Mock.Verify(x => x.SendMessageAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void TestLegionMessage()
        {
            BotMessaging bm = new(GetServiceProvider());

            var t = bm.SendLegionMessage(new[] { Villager1Mock.Object, Villager2Mock.Object, Villager3Mock.Object });
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Villager 1 should hear they're the demon and hear the other demons too
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are the **demon**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager2Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager3Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            // Villager 2 should hear they're the demon and hear the other demons too
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are the **demon**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager1Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager3Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            // Villager 3 should hear they're the demon and hear the other demons too
            Villager3Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are the **demon**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager3Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager1Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager3Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager2Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Fact]
        public void TestMagicianMessage()
        {
            BotMessaging bm = new(GetServiceProvider());

            var t = bm.SendMagicianMessage(Villager1Mock.Object, new[] { Villager2Mock.Object }, Villager3Mock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Villager 1 should hear they're the demon and hear the minion and magician, but no fellow demons
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are the **demon**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("fellow demons", StringComparison.InvariantCultureIgnoreCase))), Times.Never);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager2Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager1Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager3Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            // Villager 2 should hear they're a minion and hear 2 demons but no fellow minions
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("you are a **minion**", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("your demons are", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains("fellow minions", StringComparison.InvariantCultureIgnoreCase))), Times.Never);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager2Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            Villager2Mock.Verify(x => x.SendMessageAsync(It.Is<string>(s => s.Contains(Villager3Mock.Object.DisplayName, StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            // Villager 3 should hear nothing
            Villager3Mock.Verify(x => x.SendMessageAsync(It.IsAny<string>()), Times.Never);
        }

    }
}
