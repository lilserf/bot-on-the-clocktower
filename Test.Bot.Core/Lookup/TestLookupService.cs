using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Interaction;
using Bot.Core.Lookup;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Test.Bot.Base;
using Test.Bot.Core.Interaction;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestLookupService : TestBase
    {
        private readonly Mock<IBotInteractionContext> m_mockInteractionContext = new(MockBehavior.Strict);
        private readonly Mock<IGuildInteractionErrorHandler> m_mockGuildErrorHandler = new(MockBehavior.Strict);
        private readonly Mock<ICharacterLookup> m_mockCharacterLookup = new(MockBehavior.Strict);
        private readonly Mock<IGuild> m_mockInteractionGuild = new(MockBehavior.Strict);
        private readonly Mock<IMember> m_mockInteractionAuthor = new(MockBehavior.Strict);
        private readonly Mock<IChannel> m_mockInteractionChannel = new(MockBehavior.Strict);
        private readonly Mock<ILookupRoleDatabase> m_mockLookupDb = new(MockBehavior.Strict);
        private readonly Mock<IProcessLogger> m_mockProcessLogger = new(MockBehavior.Strict);
        private readonly Mock<ILookupEmbedBuilder> m_mockLookupEmbedBuilder = new(MockBehavior.Strict);

        private readonly List<string> m_mockDbScriptUrls = new();
        private ulong m_mockGuildId = 123ul;
        private string m_mockInteractionChannelName = "channel name";

        public TestLookupService()
        {
            RegisterMock(m_mockLookupDb);
            RegisterMock(m_mockGuildErrorHandler);
            RegisterMock(m_mockCharacterLookup);
            RegisterMock(m_mockLookupEmbedBuilder);

            m_mockLookupDb.Setup(ld => ld.AddScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.RemoveScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.GetScriptUrlsAsync(It.IsAny<ulong>())).ReturnsAsync(m_mockDbScriptUrls);
            m_mockCharacterLookup.Setup(cl => cl.LookupCharacterAsync(It.IsAny<ulong>(), It.IsAny<string>())).ReturnsAsync(new LookupCharacterResult(Enumerable.Empty<LookupCharacterItem>()));
            m_mockProcessLogger.Setup(pl => pl.LogException(It.IsAny<Exception>(), It.IsAny<string>()));

            m_mockInteractionGuild.SetupGet(g => g.Id).Returns(m_mockGuildId);

            m_mockInteractionContext.SetupGet(ic => ic.Guild).Returns(m_mockInteractionGuild.Object);
            m_mockInteractionContext.SetupGet(ic => ic.Member).Returns(m_mockInteractionAuthor.Object);
            m_mockInteractionContext.SetupGet(ic => ic.Channel).Returns(m_mockInteractionChannel.Object);

            m_mockInteractionChannel.SetupGet(c => c.Name).Returns(m_mockInteractionChannelName);
        }

        #region Interaction testing
        [Fact]
        public void LookupRequested_WrapsInteraction()
        {
            string lookupStr = "test lookup";

            TestInteractionWrapperHelper.TestGuildInteractionRequested(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.LookupAsync(m_mockInteractionContext.Object, lookupStr);
                },
                (s, ic) =>
                {
                    Assert.Contains(lookupStr, s);
                    Assert.Equal(m_mockInteractionContext.Object, ic);
                });
        }

        [Fact]
        public void AddScriptRequested_WrapsInteraction()
        {
            string scriptUrl = "test script url";

            TestInteractionWrapperHelper.TestGuildInteractionRequested(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.AddScriptAsync(m_mockInteractionContext.Object, scriptUrl);
                },
                (s, ic) =>
                {
                    Assert.Contains("adding", s, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Contains(scriptUrl, s);
                    Assert.Equal(m_mockInteractionContext.Object, ic);
                });
        }

        [Fact]
        public void RemoveScriptRequested_WrapsInteraction()
        {
            string scriptUrl = "test script url";

            TestInteractionWrapperHelper.TestGuildInteractionRequested(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.RemoveScriptAsync(m_mockInteractionContext.Object, scriptUrl);
                },
                (s, ic) =>
                {
                    Assert.Contains("removing", s, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Contains(scriptUrl, s);
                    Assert.Equal(m_mockInteractionContext.Object, ic);
                });
        }

        [Fact]
        public void ListScriptsRequested_WrapsInteraction()
        {
            TestInteractionWrapperHelper.TestGuildInteractionRequested(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.ListScriptsAsync(m_mockInteractionContext.Object);
                },
                (s, ic) =>
                {
                    Assert.Contains("scripts", s, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Equal(m_mockInteractionContext.Object, ic);
                });
        }
        #endregion

        [Fact]
        public void AddScriptRequested_AddsScriptToDbAndReturnsResults()
        {
            string scriptUrl = "test script url";

            TestInteractionWrapperHelper.TestGuildInteractionMethod(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.AddScriptAsync(m_mockInteractionContext.Object, scriptUrl);
                },
                (_, ir) =>
                {
                    Assert.Contains(scriptUrl, ir.Message);
                    Assert.Contains("added", ir.Message);
                    Assert.False(ir.IncludeComponents);
                    m_mockLookupDb.Verify(ld => ld.AddScriptUrlAsync(It.Is<ulong>(l => l == m_mockGuildId), It.Is<string>(s => s == scriptUrl)), Times.Once);
                });
        }

        [Fact]
        public void RemoveScriptRequested_RemovesScriptFromDbAndReturnsResults()
        {
            string scriptUrl = "test script url";

            TestInteractionWrapperHelper.TestGuildInteractionMethod(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.RemoveScriptAsync(m_mockInteractionContext.Object, scriptUrl);
                },
                (_, ir) =>
                {
                    Assert.Contains(scriptUrl, ir.Message);
                    Assert.Contains("removed", ir.Message);
                    Assert.False(ir.IncludeComponents);
                    m_mockLookupDb.Verify(ld => ld.RemoveScriptUrlAsync(It.Is<ulong>(l => l == m_mockGuildId), It.Is<string>(s => s == scriptUrl)), Times.Once);
                });
        }

        [Fact]
        public void NoScripts_ListScriptsRequested_ReturnsNoScripts()
        {
            TestInteractionWrapperHelper.TestGuildInteractionMethod(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.ListScriptsAsync(m_mockInteractionContext.Object);
                },
                (_, ir) =>
                {
                    Assert.Contains("no custom scripts", ir.Message, StringComparison.InvariantCultureIgnoreCase);
                    Assert.False(ir.IncludeComponents);
                });
        }

        [Fact]
        public void SomeScripts_ListScriptsRequested_ReturnsScripts()
        {
            string script1 = "script 1 url";
            string script2 = "script 2 url";
            m_mockDbScriptUrls.Add(script1);
            m_mockDbScriptUrls.Add(script2);

            TestInteractionWrapperHelper.TestGuildInteractionMethod(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.ListScriptsAsync(m_mockInteractionContext.Object);
                },
                (_, ir) =>
                {
                    Assert.Contains("custom scripts", ir.Message, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Contains("found", ir.Message, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Contains($"⦁ {script1}", ir.Message);
                    Assert.Contains($"⦁ {script2}", ir.Message);
                    Assert.False(ir.IncludeComponents);
                });
        }

        [Fact]
        public void LookupNoCharacters_MessageAboutNoneFound()
        {
            string lookupStr = "lookup str";

            TestInteractionWrapperHelper.TestGuildInteractionMethod(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.LookupAsync(m_mockInteractionContext.Object, lookupStr);
                },
                (_, ir) =>
                {
                    Assert.Contains("no ", ir.Message, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Contains("found", ir.Message, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Contains(lookupStr, ir.Message);
                    Assert.Empty(ir.Embeds);
                });
        }

        [Fact]
        public void LookupMultipleCharacters_AddsEmbedsToResult()
        {
            string lookupStr = "lookup str";

            var testItem1 = new LookupCharacterItem(new CharacterData("char1id", "char1", "abil1", CharacterTeam.Fabled, isOfficial: true), Enumerable.Empty<ScriptData>());
            var testItem2 = new LookupCharacterItem(new CharacterData("char2id", "char2", "abil2", CharacterTeam.Townsfolk, isOfficial: false), Enumerable.Empty<ScriptData>());
            var expectedMessageCharacters = new[] { testItem1, testItem2 };

            m_mockCharacterLookup.Setup(cl => cl.LookupCharacterAsync(It.Is<ulong>(l => l == m_mockGuildId), It.Is<string>(s => s == lookupStr))).ReturnsAsync(new LookupCharacterResult(expectedMessageCharacters));

            List<LookupCharacterItem> actualMessageCharacters = new();
            IEmbed[] expectedEmbedObjects = new[] { new Mock<IEmbed>(MockBehavior.Strict).Object, new Mock<IEmbed>(MockBehavior.Strict).Object };
            m_mockLookupEmbedBuilder
                .Setup(leb => leb.BuildLookupEmbed(It.Is<LookupCharacterItem>(lci => lci == testItem1)))
                .Callback<LookupCharacterItem>(actualMessageCharacters.Add)
                .Returns(expectedEmbedObjects[0]);
            m_mockLookupEmbedBuilder
                .Setup(leb => leb.BuildLookupEmbed(It.Is<LookupCharacterItem>(lci => lci == testItem2)))
                .Callback<LookupCharacterItem>(actualMessageCharacters.Add)
                .Returns(expectedEmbedObjects[1]);

            TestInteractionWrapperHelper.TestGuildInteractionMethod(GetServiceProvider(),
                (sp) =>
                {
                    BotLookupService bls = new(sp);
                    return bls.LookupAsync(m_mockInteractionContext.Object, lookupStr);
                },
                (_, ir) =>
                {
                    Assert.Contains("found", ir.Message, StringComparison.InvariantCultureIgnoreCase);
                    Assert.Contains(expectedMessageCharacters.Length.ToString(), ir.Message);
                    Assert.Contains(lookupStr, ir.Message);
                    Assert.Equal(expectedEmbedObjects, ir.Embeds);
                    Assert.Equal(expectedMessageCharacters, actualMessageCharacters);
                });
        }
    }
}
