﻿using Bot.Api.Database;
using Bot.Core.Lookup;
using Moq;
using System.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterStorage : TestBase
    {
        private readonly Mock<IOfficialCharacterCache> m_mockOfficialCache = new(MockBehavior.Strict);
        private readonly Mock<ICustomScriptCache> m_mockCustomCache = new(MockBehavior.Strict);
        private readonly Mock<ILookupRoleDatabase> m_mockLookupDb = new(MockBehavior.Strict);

        public TestCharacterStorage()
        {
            RegisterMock(m_mockOfficialCache);
            RegisterMock(m_mockCustomCache);
            RegisterMock(m_mockLookupDb);

            m_mockOfficialCache.Setup(oc => oc.GetOfficialCharactersAsync()).ReturnsAsync(new GetOfficialCharactersResult(Enumerable.Empty<GetOfficialCharactersItem>()));
            m_mockOfficialCache.Setup(oc => oc.InvalidateCache());
            m_mockCustomCache.Setup(cc => cc.GetCustomScriptAsync(It.IsAny<string>())).ReturnsAsync(new GetCustomScriptResult(Enumerable.Empty<ScriptWithCharacters>()));
            m_mockCustomCache.Setup(cc => cc.InvalidateCache(It.IsAny<string>()));
            m_mockLookupDb.Setup(ld => ld.GetScriptUrlsAsync(It.IsAny<ulong>())).ReturnsAsync(new string[] { });
        }

        [Fact]
        public void CharacterStorage_GetOfficalCharacters_ReturnsCharacterWithScript()
        {
            var expectedScript = new ScriptData("test script", isOfficial: true);
            var expectedChar = new CharacterData("testid", "test char", "test ability", CharacterTeam.Minion, isOfficial: true);

            m_mockOfficialCache.Setup(sc => sc.GetOfficialCharactersAsync()).ReturnsAsync(new GetOfficialCharactersResult(new[] { new GetOfficialCharactersItem(expectedChar, new[] { expectedScript }) }));

            var cs = new CharacterStorage(GetServiceProvider());

            var actualResult = AssertCompletedTask(() => cs.GetCharactersAsync(0ul));

            Assert.Collection(actualResult.Items,
                i =>
                {
                    Assert.Equal(expectedChar, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(expectedScript, s));
                });
        }

        [Fact]
        public void CharacterStorage_GetCustomScript_ReturnsCharacterWithScript()
        {
            ulong testGuildId = 123ul;
            string testUrl1 = "test url 1";
            string testUrl2 = "test url 2";

            m_mockLookupDb.Setup(ld => ld.GetScriptUrlsAsync(It.Is<ulong>(g => g == testGuildId))).ReturnsAsync(new string[] { testUrl1, testUrl2 });

            var expectedScript1 = new ScriptData("test script 1", isOfficial: false);
            var expectedChar1 = new CharacterData("testid1", "test char 1", "test ability 1", CharacterTeam.Townsfolk, isOfficial: false);

            var expectedScript2 = new ScriptData("test script 1", isOfficial: false);
            var expectedChar2 = new CharacterData("testid2", "test char 2", "test ability 2", CharacterTeam.Demon, isOfficial: false);
            var expectedChar3 = new CharacterData("testid3", "test char 3", "test ability 3", CharacterTeam.Fabled, isOfficial: false);

            m_mockCustomCache.Setup(cc => cc.GetCustomScriptAsync(It.Is<string>(s => s == testUrl1))).ReturnsAsync(new GetCustomScriptResult(new[] { new ScriptWithCharacters(expectedScript1, new[] { expectedChar1 }) }));
            m_mockCustomCache.Setup(cc => cc.GetCustomScriptAsync(It.Is<string>(s => s == testUrl2))).ReturnsAsync(new GetCustomScriptResult(new[] { new ScriptWithCharacters(expectedScript2, new[] { expectedChar2, expectedChar3 }) }));

            var cs = new CharacterStorage(GetServiceProvider());

            var actualResult = AssertCompletedTask(() => cs.GetCharactersAsync(testGuildId));

            Assert.Collection(actualResult.Items,
                i =>
                {
                    Assert.Equal(expectedChar1, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(expectedScript1, s));
                },
                i =>
                {
                    Assert.Equal(expectedChar2, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(expectedScript2, s));
                },
                i =>
                {
                    Assert.Equal(expectedChar3, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(expectedScript2, s));
                }
            );
        }

        [Fact]
        public void CharacterStorage_RefreshScripts_InvalidatesExpectedScripts()
        {
            ulong testGuildId = 123ul;
            string testUrl1 = "test url 1";
            string testUrl2 = "test url 2";

            m_mockLookupDb.Setup(ld => ld.GetScriptUrlsAsync(It.Is<ulong>(g => g == testGuildId))).ReturnsAsync(new string[] { testUrl1, testUrl2 });

            var cs = new CharacterStorage(GetServiceProvider());
            m_mockOfficialCache.Verify(oc => oc.InvalidateCache(), Times.Never);
            m_mockCustomCache.Verify(cc => cc.InvalidateCache(It.IsAny<string>()), Times.Never);

            AssertCompletedTask(() => cs.RefreshCharactersAsync(testGuildId));

            m_mockOfficialCache.Verify(oc => oc.InvalidateCache(), Times.Once);
            m_mockOfficialCache.VerifyNoOtherCalls();
            m_mockCustomCache.Verify(cc => cc.InvalidateCache(It.Is<string>(s => s == testUrl1)), Times.Once);
            m_mockCustomCache.Verify(cc => cc.InvalidateCache(It.Is<string>(s => s == testUrl2)), Times.Once);
            m_mockCustomCache.VerifyNoOtherCalls();
        }
    }
}
