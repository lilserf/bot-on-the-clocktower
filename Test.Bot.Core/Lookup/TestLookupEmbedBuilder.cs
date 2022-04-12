using Bot.Api;
using Bot.Core.Lookup;
using Moq;
using System;
using System.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestLookupEmbedBuilder : TestBase, IDisposable
    {
        private readonly Mock<IBotSystem> m_botSystem = new(MockBehavior.Strict);
        private readonly Mock<IEmbedBuilder> m_mockEmbedBuilder = new(MockBehavior.Strict);
        private readonly Mock<IColorBuilder> m_mockColorBuilder = new(MockBehavior.Strict);
        private readonly Mock<IEmbed> m_mockEmbed = new(MockBehavior.Strict);

        public TestLookupEmbedBuilder()
        {
            RegisterMock(m_botSystem);

            m_botSystem.Setup(bs => bs.CreateEmbedBuilder()).Returns(m_mockEmbedBuilder.Object);
            m_botSystem.SetupGet(bs => bs.ColorBuilder).Returns(m_mockColorBuilder.Object);

            m_mockColorBuilder.Setup(cb => cb.Build(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<byte>())).Returns(new Mock<IColor>().Object);

            m_mockEmbedBuilder.Setup(eb => eb.Build()).Returns(m_mockEmbed.Object);
            m_mockEmbedBuilder.Setup(eb => eb.WithTitle(It.IsAny<string>())).Returns(m_mockEmbedBuilder.Object);
            m_mockEmbedBuilder.Setup(eb => eb.WithDescription(It.IsAny<string>())).Returns(m_mockEmbedBuilder.Object);
            m_mockEmbedBuilder.Setup(eb => eb.AddField(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(m_mockEmbedBuilder.Object);
            m_mockEmbedBuilder.Setup(eb => eb.WithColor(It.IsAny<IColor>())).Returns(m_mockEmbedBuilder.Object);
            m_mockEmbedBuilder.Setup(eb => eb.WithFooter(It.IsAny<string>(), It.IsAny<string>())).Returns(m_mockEmbedBuilder.Object);
            m_mockEmbedBuilder.Setup(eb => eb.WithThumbnail(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(m_mockEmbedBuilder.Object);
        }

        public void Dispose()
        {
            m_botSystem.Verify(bs => bs.CreateEmbedBuilder(), Times.AtMostOnce); ;
            m_botSystem.VerifyGet(bs => bs.ColorBuilder, Times.AtMostOnce);
        }

        [Fact]
        public void PassedCharacter_ReturnsEmbedWithRequiredCharacterFields()
        {
            string charId = "characterId";
            string charName = "Character Name";
            string charAbility = "Character Ability";
            var c = new CharacterData(charId, charName, charAbility, CharacterTeam.Outsider, isOfficial: false);

            var leb = new LookupEmbedBuilder(GetServiceProvider());
            var embed = leb.BuildLookupEmbed(new LookupCharacterItem(c, Enumerable.Empty<ScriptData>()));

            Assert.Equal(m_mockEmbed.Object, embed);
            m_mockEmbedBuilder.Verify(eb => eb.WithTitle(It.Is<string>(s => s.Contains(charName))), Times.Once);
            m_mockEmbedBuilder.Verify(eb => eb.WithDescription(It.Is<string>(s => s.Contains("Outsider", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            m_mockEmbedBuilder.Verify(eb => eb.AddField(It.Is<string>(s => s.Contains("Ability", StringComparison.InvariantCultureIgnoreCase)), It.Is<string>(s => s.Contains(charAbility)), It.Is<bool>(b => b == false)), Times.Once);
        }

        [Fact]
        public void PassedCharacter_ReturnsEmbedWithFlavorText()
        {
            string flavorText = "Flavor Text";
            var c = CreateBasicCharacter();
            c.FlavorText = flavorText;

            var leb = new LookupEmbedBuilder(GetServiceProvider());
            var embed = leb.BuildLookupEmbed(new LookupCharacterItem(c, Enumerable.Empty<ScriptData>()));

            Assert.Equal(m_mockEmbed.Object, embed);
            m_mockEmbedBuilder.Verify(eb => eb.WithFooter(It.Is<string>(s => s.Contains(flavorText)), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void PassedScripts_OutputsScriptNames()
        {
            var script1 = new ScriptData("Script 1", isOfficial: false);
            var script2 = new ScriptData("Script 2", isOfficial: false);

            var leb = new LookupEmbedBuilder(GetServiceProvider());
            var embed = leb.BuildLookupEmbed(new LookupCharacterItem(CreateBasicCharacter(), new[] { script1, script2 }));

            Assert.Equal(m_mockEmbed.Object, embed);
            m_mockEmbedBuilder.Verify(eb => eb.AddField(
                It.Is<string>(s => s.Contains("Found In", StringComparison.InvariantCultureIgnoreCase)),
                It.Is<string>(s => s.Contains(script1.Name) && s.Contains(script2.Name)),
                It.Is<bool>(b => b == false)), Times.Once);
        }

        [Fact]
        public void PassedOfficialScript_UsesAlmanacLink()
        {
            var officialScript = new ScriptData("Trouble Brewing", isOfficial: true);
            officialScript.AlmanacUrl = "almanac_url";

            var leb = new LookupEmbedBuilder(GetServiceProvider());
            var embed = leb.BuildLookupEmbed(new LookupCharacterItem(CreateBasicCharacter(isOfficial:true), new[] { officialScript }));

            Assert.Equal(m_mockEmbed.Object, embed);

            m_mockEmbedBuilder.Verify(eb => eb.WithDescription(It.Is<string>(s => s.Contains(" (Official)"))), Times.Once);
            m_mockEmbedBuilder.Verify(eb => eb.AddField(
                It.Is<string>(s => s.Contains("Found In", StringComparison.InvariantCultureIgnoreCase)),
                It.Is<string>(s => s.Contains($"[{officialScript.Name}]({officialScript.AlmanacUrl})")),
                It.Is<bool>(b => b == false)), Times.Once);
        }

        [Fact]
        public void PassedNoScript_NoFoundInFields()
        {
            var leb = new LookupEmbedBuilder(GetServiceProvider());
            var embed = leb.BuildLookupEmbed(new LookupCharacterItem(CreateBasicCharacter(), Enumerable.Empty<ScriptData>()));

            Assert.Equal(m_mockEmbed.Object, embed);
            m_mockEmbedBuilder.Verify(eb => eb.AddField(It.Is<string>(s => s.Contains("Found In", StringComparison.InvariantCultureIgnoreCase)), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void ScriptAuthorsProvided_OutputsAuthor()
        {
            var script1 = new ScriptData("Script 1", isOfficial: true);
            string author1 = "The Pandemonium Institute";
            script1.Author = author1;

            var script2 = new ScriptData("Script 2", isOfficial: false);
            string author2 = "some person";
            script2.Author = author2;            

            var leb = new LookupEmbedBuilder(GetServiceProvider());
            var embed = leb.BuildLookupEmbed(new LookupCharacterItem(CreateBasicCharacter(), new[] { script1, script2 }));

            Assert.Equal(m_mockEmbed.Object, embed);
            m_mockEmbedBuilder.Verify(eb => eb.AddField(
                It.Is<string>(s => s.Contains("Found In", StringComparison.InvariantCultureIgnoreCase)),
                It.Is<string>(s => s.Contains($"by {script1.Author}", StringComparison.InvariantCultureIgnoreCase) && s.Contains($"by {script2.Author}", StringComparison.InvariantCultureIgnoreCase)),
                It.Is<bool>(b => b == false)), Times.Once);
        }

        private static CharacterData CreateBasicCharacter(bool isOfficial=false) => new("charid", "charname", "charAbility", CharacterTeam.Townsfolk, isOfficial: isOfficial);
    }
}
