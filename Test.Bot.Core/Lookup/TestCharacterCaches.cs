using Bot.Core.Lookup;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterCaches : TestBase
    {
        private const string ValidMetaJson = "{\"id\":\"_meta\",\"name\":\"script name\"}";

        private readonly Mock<IStringDownloader> m_mockDownloader = new(MockBehavior.Strict);

        public TestCharacterCaches()
        {
            RegisterMock(m_mockDownloader);
        }

        [Fact]
        public void CustomScript_ErrorRetieving_ReturnsNoResults()
        {
            SetupDownload(null);

            var result = PerformGet();

            Assert.Empty(result.ScriptsWithCharacters);
        }

        [Fact]
        public void CustomScript_NoMeta_ReturnsNoResults()
        {
            SetupDownload("[]");

            var result = PerformGet();

            Assert.Empty(result.ScriptsWithCharacters);
        }

        [Fact]
        public void CustomScript_UnnamedMeta_ReturnsNoResults()
        {
            SetupDownload($"[{{\"id\":\"_meta\"}}]");

            var result = PerformGet();

            Assert.Empty(result.ScriptsWithCharacters);
        }

        [Fact]
        public void CustomScript_JustMeta_ReturnsValidScriptNoCharacters()
        {
            string scriptName = "script name";
            string almanacUrl = "almanac url";
            SetupDownload($"[{{\"id\":\"_meta\",\"name\":\"{scriptName}\",\"almanac\":\"{almanacUrl}\"}}]");
            
            var result = PerformGet();

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
            SetupDownload(json);

            var result = PerformGet();

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
            SetupDownload($"[{ValidMetaJson},{char1Json},{char2Json}]");

            var result = PerformGet();

            Assert.Collection(result.ScriptsWithCharacters,
                swc =>
                {
                    Assert.Collection(swc.Characters,
                        c =>
                        {
                            Assert.Equal(char1Name, c.Name);
                            Assert.Equal(char1Ability, c.Ability);
                            Assert.False(c.IsOfficial);
                        },
                        c =>
                        {
                            Assert.Equal(char2Name, c.Name);
                            Assert.Equal(char2Ability, c.Ability);
                            Assert.False(c.IsOfficial);
                        });
                });
        }

        [Theory]
        [InlineData("{\"ability\":\"some ability\",\"team\":\"townsfolk\"}")]
        [InlineData("{\"name\":\"some name\",\"team\":\"townsfolk\"}")]
        [InlineData("{\"name\":\"some name\",\"ability\":\"some ability\"}")]
        public void CustomScript_CharMissingRequiredData_NoChars(string charJson)
        {
            SetupDownload($"[{ValidMetaJson},{charJson}]");

            var result = PerformGet();

            Assert.Collection(result.ScriptsWithCharacters,
                swc =>
                {
                    Assert.Empty(swc.Characters);
                });
        }

        /*
         * TODO: Test these properly 
        public CharacterTeam Team { get; }  <-- Theory: all types work
        public string? FlavorText { get; set; }
        public string? ImageUrl { get; set; }
        */

        private GetCustomScriptResult PerformGet()
        {
            var csc = new CustomScriptCache(GetServiceProvider());
            return AssertCompletedTask(() => csc.GetCustomScriptAsync("url"));
        }

        private void SetupDownload(string? data)
        {
            m_mockDownloader.Setup(d => d.DownloadStringAsync(It.IsAny<string>())).ReturnsAsync(new DownloadResult(data));
        }
    }
}
