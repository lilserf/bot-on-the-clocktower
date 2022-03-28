using Bot.Api.Lookup;
using Bot.Core.Lookup;
using Newtonsoft.Json.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestJsonParsing : TestBase
    {
        private const string ValidMetaJson = "{\"id\":\"_meta\",\"name\":\"script name\"}";

        [Fact]
        public void CustomScript_NoMeta_ReturnsNoResults()
        {
            var result = PerformCustomParse("[]");

            Assert.Empty(result.ScriptsWithCharacters);
        }

        [Fact]
        public void CustomScript_UnnamedMeta_ReturnsNoResults()
        {
            var result = PerformCustomParse("[{{\"id\":\"_meta\"}}]");

            Assert.Empty(result.ScriptsWithCharacters);
        }

        [Fact]
        public void CustomScript_JustMeta_ReturnsValidScriptNoCharacters()
        {
            string scriptName = "script name";
            string almanacUrl = "almanac url";

            var result = PerformCustomParse($"[{{\"id\":\"_meta\",\"name\":\"{scriptName}\",\"almanac\":\"{almanacUrl}\"}}]");

            Assert.Collection(result.ScriptsWithCharacters,
                swc =>
                {
                    Assert.Equal(scriptName, swc.Script.Name);
                    Assert.Equal(almanacUrl, swc.Script.AlmanacUrl);
                    Assert.False(swc.Script.IsOfficial);
                    Assert.Empty(swc.Characters);
                });
        }

        [Theory]
        [InlineData("this is not a valid array, it's a string")]
        [InlineData("{\"name\":\"this is not a valid array, it's an object\"}")]
        public void CustomScript_NotValidArray_EmptyResult(string json)
        {
            var result = PerformCustomParse(json);

            Assert.Empty(result.ScriptsWithCharacters);
        }

        [Fact]
        public void CustomScript_2Characters_ReturnsBoth()
        {
            string char1Name = "char1 name";
            string char2Name = "char2 name";
            string char1Ability = "char1 ability";
            string char2Ability = "char2 ability";

            string char1Json = $"{{\"name\":\"{char1Name}\",\"ability\":\"{char1Ability}\",\"team\":\"townsfolk\"}}";
            string char2Json = $"{{\"name\":\"{char2Name}\",\"ability\":\"{char2Ability}\",\"team\":\"townsfolk\"}}";

            var result = PerformCustomParse($"[{ValidMetaJson},{char1Json},{char2Json}]");

            Assert.Collection(result.ScriptsWithCharacters,
                swc =>
                {
                    Assert.Collection(swc.Characters,
                        c =>
                        {
                            Assert.Equal(char1Name, c.Name);
                            Assert.Equal(char1Ability, c.Ability);
                            Assert.Equal(CharacterTeam.Townsfolk, c.Team);
                            Assert.False(c.IsOfficial);
                        },
                        c =>
                        {
                            Assert.Equal(char2Name, c.Name);
                            Assert.Equal(char2Ability, c.Ability);
                            Assert.Equal(CharacterTeam.Townsfolk, c.Team);
                            Assert.False(c.IsOfficial);
                        });
                });
        }

        [Theory]
        [InlineData("{\"ability\":\"some ability\",\"team\":\"townsfolk\"}")]
        [InlineData("{\"name\":\"some name\",\"team\":\"townsfolk\"}")]
        [InlineData("{\"name\":\"some name\",\"ability\":\"some ability\"}")]
        public void CharParse_CharMissingRequiredData_NoChars(string charJson)
        {
            var result = JsonParseUtil.ParseCharacterData(JObject.Parse(charJson), isOfficial: false);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("townsfolk", CharacterTeam.Townsfolk)]
        [InlineData("outsider", CharacterTeam.Outsider)]
        [InlineData("minion", CharacterTeam.Minion)]
        [InlineData("demon", CharacterTeam.Demon)]
        [InlineData("traveler", CharacterTeam.Traveler)]
        [InlineData("fabled", CharacterTeam.Fabled)]
        public void CharParse_CharWithTeam_MatchesEnum(string teamStr, CharacterTeam expectedTeam)
        {
            var charJson = $"{{\"name\":\"some name\",\"ability\":\"some ability\",\"team\":\"{teamStr}\"}}";

            var result = JsonParseUtil.ParseCharacterData(JObject.Parse(charJson), isOfficial: false);

            Assert.NotNull(result);
            Assert.Equal(expectedTeam, result!.Team);
        }

        [Fact]
        public void CharParse_InvalidTeam_NullChar()
        {
            var charJson = $"{{\"name\":\"some name\",\"ability\":\"some ability\",\"team\":\"invalidteam\"}}";

            var result = JsonParseUtil.ParseCharacterData(JObject.Parse(charJson), isOfficial: false);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CharParse_IsOfficial_Set(bool isOfficial)
        {
            var result = ParseSimpleCharacter(isOfficial: isOfficial);
            Assert.Equal(isOfficial, result.IsOfficial);
        }

        [Fact]
        public void CharParse_NoFlavor_FlavorNull()
        {
            var result = ParseSimpleCharacter(isOfficial: false);
            Assert.Null(result.FlavorText);
        }

        [Fact]
        public void CharParse_NoImage_ImageNull()
        {
            var result = ParseSimpleCharacter(isOfficial: false);
            Assert.Null(result.ImageUrl);
        }

        [Fact]
        public void CharParse_FlavorProvided_IsSet()
        {
            var flavor = "some flavor text";
            var result = ParseSimpleCharacter(isOfficial: false, $"\"flavor\":\"{flavor}\"");
            Assert.Equal(flavor, result.FlavorText);
        }

        [Fact]
        public void CharParse_ImageProvided_IsSet()
        {
            var imageUrl = "some image url";
            var result = ParseSimpleCharacter(isOfficial: false, $"\"image\":\"{imageUrl}\"");
            Assert.Equal(imageUrl, result.ImageUrl);
        }

        private static CharacterData ParseSimpleCharacter(bool isOfficial, string? addition=null)
        {
            var append = (addition != null) ? $",{addition}" : string.Empty;
            var charJson = $"{{\"name\":\"some name\",\"ability\":\"some ability\",\"team\":\"townsfolk\"{append}}}";
            var result = JsonParseUtil.ParseCharacterData(JObject.Parse(charJson), isOfficial: isOfficial);
            Assert.NotNull(result);
            return result!;
        }

        private static GetCustomScriptResult PerformCustomParse(string json)
        {
            var csp = new CustomScriptParser();
            return csp.ParseScript(json);
        }
    }
}
