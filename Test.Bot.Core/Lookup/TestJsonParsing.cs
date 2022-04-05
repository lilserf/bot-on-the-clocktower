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
            string author = "author name";

            var result = PerformCustomParse($"[{{\"id\":\"_meta\",\"name\":\"{scriptName}\",\"almanac\":\"{almanacUrl}\",\"author\":\"{author}\"}}]");

            Assert.Collection(result.ScriptsWithCharacters,
                swc =>
                {
                    Assert.Equal(scriptName, swc.Script.Name);
                    Assert.Equal(almanacUrl, swc.Script.AlmanacUrl);
                    Assert.Equal(author, swc.Script.Author);
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

            string char1Json = $"{{\"id\":\"someid1\",\"name\":\"{char1Name}\",\"ability\":\"{char1Ability}\",\"team\":\"townsfolk\"}}";
            string char2Json = $"{{\"id\":\"someid2\",\"name\":\"{char2Name}\",\"ability\":\"{char2Ability}\",\"team\":\"townsfolk\"}}";

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

        private const string TbId = "tb";
        private const string TbName = "Trouble Brewing";
        private const string OfficialAuthor = "Trouble Brewing";
        private const string TbAlmanac = OfficialWikiHelper.WikiPrefixUrl + "Trouble_Brewing";
        private static readonly string TbJson = $"{{\"id\":\"{TbId}\",\"name\":\"{TbName}\",\"author\":\"{OfficialAuthor}\",\"roles\":[]}}";

        private const string PoisonerId = "poisoner";
        private const string PoisonerName = "Poisoner";
        private const string PoisonerAbility = "Each night, choose a player: they are poisoned tonight and tomorrow day.";
        private static readonly string PoisonerJson = $"{{\"id\":\"{PoisonerId}\",\"name\":\"{PoisonerName}\",\"ability\":\"{PoisonerAbility}\",\"team\":\"minion\",\"edition\":\"{TbId}\"}}";

        private const string SavantId = "savant";
        private const string SavantName = "Savant";
        private const string SavantAbility = "Each day, you may visit the Storyteller to learn 2 things in private: 1 is true & 1 is false.";
        private static readonly string SavantJson = $"{{\"id\":\"{SavantId}\",\"name\":\"{SavantName}\",\"ability\":\"{SavantAbility}\",\"team\":\"townsfolk\",\"edition\":\"{TbId}\"}}";

        private const string LufId = "luf";
        private const string LufName = "Laissez un Faire";
        private static readonly string LufJson = $"{{\"id\":\"{LufId}\",\"name\":\"{LufName}\",\"author\":\"{OfficialAuthor}\",\"roles\":[\"{SavantId}\"]}}";

        private const string UnofficialName = "Unofficial";
        private static readonly string UnofficialJson = $"{{\"name\":\"{UnofficialName}\",\"isOfficial\":false,\"roles\":[\"{SavantId}\"]}}";


        [Fact]
        public void OfficialParse_JustCharacters_ReturnsCharacters()
        {
            var op = new OfficialScriptParser();
            var result = op.ParseOfficialData(new string[] { }, new[] { $"[{PoisonerJson}]", $"[{SavantJson}]" });

            Assert.Collection(result.Items,
                i =>
                {
                    Assert.Equal(PoisonerName, i.Character.Name);
                    Assert.Equal(PoisonerAbility, i.Character.Ability);
                    Assert.Equal(CharacterTeam.Minion, i.Character.Team);
                    Assert.True(i.Character.IsOfficial);
                    Assert.Empty(i.Scripts);
                },
                i =>
                {
                    Assert.Equal(SavantName, i.Character.Name);
                    Assert.Equal(SavantAbility, i.Character.Ability);
                    Assert.Equal(CharacterTeam.Townsfolk, i.Character.Team);
                    Assert.True(i.Character.IsOfficial);
                    Assert.Empty(i.Scripts);
                });
        }

        [Fact]
        public void OfficialParse_CharacterInScript_ReturnsCharacterInScript()
        {
            var op = new OfficialScriptParser();
            var result = op.ParseOfficialData(new[] { $"[{TbJson}]" }, new[] { $"[{PoisonerJson}]" });

            Assert.Collection(result.Items,
                i =>
                {
                    Assert.Equal(PoisonerName, i.Character.Name);
                    Assert.Collection(i.Scripts,
                        s =>
                        {
                            Assert.True(s.IsOfficial);
                            Assert.Equal(TbName, s.Name);
                            Assert.Equal(TbAlmanac, s.AlmanacUrl);
                            Assert.Equal(OfficialAuthor, s.Author);
                        });
                });
        }

        [Fact]
        public void OfficialParse_InvalidScriptJson_Continues()
        {
            var op = new OfficialScriptParser();
            var result = op.ParseOfficialData(new[] { $"[\"this is invalid data\"]" }, new[] { $"[{PoisonerJson}]" });

            Assert.Collection(result.Items,
                i =>
                {
                    Assert.Equal(PoisonerName, i.Character.Name);
                });
        }

        [Fact]
        public void OfficialParse_InvalidCharacterJson_Continues()
        {
            var op = new OfficialScriptParser();
            var result = op.ParseOfficialData(new string[] {}, new[] { $"[\"invalid role json\",{PoisonerJson},\"another invalid role json\"]" });

            Assert.Collection(result.Items,
                i =>
                {
                    Assert.Equal(PoisonerName, i.Character.Name);
                });
        }

        [Fact]
        public void OfficialParse_TeensyvilleScript_FindsCharacter()
        {
            var op = new OfficialScriptParser();
            var result = op.ParseOfficialData(new[] { $"[{LufJson}]" }, new[] { $"[{SavantJson}]" });

            Assert.Collection(result.Items,
                i =>
                {
                    Assert.Equal(SavantName, i.Character.Name);
                    Assert.Equal(SavantAbility, i.Character.Ability);
                    Assert.Equal(CharacterTeam.Townsfolk, i.Character.Team);
                    Assert.True(i.Character.IsOfficial);

                    Assert.Collection(i.Scripts,
                        s =>
                        {
                            Assert.Equal(LufName, s.Name);
                            Assert.Equal(OfficialAuthor, s.Author);
                            Assert.True(s.IsOfficial);
                            Assert.Null(s.AlmanacUrl);
                        });
                });
        }

        [Fact]
        public void OfficialParse_UnofficialScript_MarkedUnofficial()
        {
            var op = new OfficialScriptParser();
            var result = op.ParseOfficialData(new[] { $"[{UnofficialJson}]" }, new[] { $"[{SavantJson}]" });

            Assert.Collection(result.Items,
                i =>
                {
                    Assert.Equal(SavantName, i.Character.Name);
                    Assert.Collection(i.Scripts,
                        s =>
                        {
                            Assert.Equal(UnofficialName, s.Name);
                            Assert.False(s.IsOfficial);
                            Assert.Null(s.Author);
                            Assert.Null(s.AlmanacUrl);
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
            var charJson = $"{{\"id\":\"someid\",\"name\":\"some name\",\"ability\":\"some ability\",\"team\":\"{teamStr}\"}}";

            var result = JsonParseUtil.ParseCharacterData(JObject.Parse(charJson), isOfficial: false);

            Assert.NotNull(result);
            Assert.Equal(expectedTeam, result!.Team);
        }

        [Fact]
        public void CharParse_InvalidTeam_NullChar()
        {
            var charJson = $"{{\"id\":\"someid\",\"name\":\"some name\",\"ability\":\"some ability\",\"team\":\"invalidteam\"}}";

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
            var charJson = $"{{\"id\":\"someid\",\"name\":\"some name\",\"ability\":\"some ability\",\"team\":\"townsfolk\"{append}}}";
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
