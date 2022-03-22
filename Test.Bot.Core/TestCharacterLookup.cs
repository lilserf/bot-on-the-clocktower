using Bot.Core.Lookup;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCharacterLookup : TestBase
    {
        private const ulong GuildId = 123ul;

        [Fact]
        public void CharacterLookup_BogusRequest_NoResults()
        {
            var lookup = new CharacterLookup(GetServiceProvider());

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "this is a bogus character request"));

            Assert.Null(result);
        }
    }
}
