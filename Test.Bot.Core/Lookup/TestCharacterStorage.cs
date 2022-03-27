using Bot.Api.Lookup;
using Bot.Core.Lookup;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterStorage : TestBase
    {
        private readonly Mock<IOfficialCharacterCache> m_mockOfficialCache = new(MockBehavior.Strict);

        public TestCharacterStorage()
        {
            RegisterMock(m_mockOfficialCache);


        }

        [Fact]
        public void CharacterSotrage_GetOfficalScripts_ReturnsCharacterWithScript()
        {
            var expectedScript = new ScriptData("test script", isOfficial: true);
            var expectedChar = new CharacterData("test char", "test ability", CharacterTeam.Minion, isOfficial: true);

            m_mockOfficialCache.Setup(sc => sc.GetOfficialCharactersAsync()).ReturnsAsync(new GetOfficialCharactersResult(new[] { new GetOfficialCharactersItem(expectedChar, new[] { expectedScript }) }));

            var cs = new CharacterStorage(GetServiceProvider());

            var actualResult = AssertCompletedTask(() => cs.GetCharactersAsync(It.IsAny<ulong>()));

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
