using Bot.Core.Lookup;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterCaches : TestBase
    {
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
