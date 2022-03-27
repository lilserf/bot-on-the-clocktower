using System;
using Bot.Core.Lookup;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterCaches : TestBase
    {
        private readonly Mock<IUrlDownloader> m_mockDownloader = new(MockBehavior.Strict);

        public TestCharacterCaches()
        {
            RegisterMock(m_mockDownloader);
        }

        [Fact]
        public void CustomScript_ErrorRetieving_ReturnsNoCharacters()
        {
            m_mockDownloader.Setup(d => d.DownloadUrlAsync(It.IsAny<string>())).ReturnsAsync(new DownloadResult(null));

            var csc = new CustomScriptCache(GetServiceProvider());
            var result = AssertCompletedTask(() => csc.GetCustomScriptAsync("url"));

            Assert.Empty(result.ScriptsWithCharacters);
        }
    }
}
