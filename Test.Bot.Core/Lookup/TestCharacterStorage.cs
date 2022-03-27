using Bot.Api.Lookup;
using Bot.Core.Lookup;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterStorage : TestBase
    {
        private readonly Mock<IScriptCache> m_mockScriptCache = new(MockBehavior.Strict);

        public TestCharacterStorage()
        {
            RegisterMock(m_mockScriptCache);
        }

        [Fact(Skip="NYI")]
        public void CharacterSotrage_GetOfficalScripts_ReturnsCharacterWithScript()
        {
            var expectedScript = new ScriptData("test script", isOfficial: true);
            var expectedChar = new CharacterData("test char", "test ability", CharacterTeam.Minion, isOfficial: true);

            m_mockScriptCache.Setup(sc => sc.GetOfficialScriptsAsync()).ReturnsAsync(new GetScriptsResult(new[] { new ScriptWithCharacters(expectedScript, new[] { expectedChar }) }));

            var cs = new CharacterStorage(GetServiceProvider());

            var actualResult = AssertCompletedTask(() => cs.GetOfficialScriptCharactersAsync());

            Assert.Collection(actualResult.Items,
                i =>
                {
                    Assert.Equal(expectedChar, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(expectedScript, s));
                });
        }
    }
}
