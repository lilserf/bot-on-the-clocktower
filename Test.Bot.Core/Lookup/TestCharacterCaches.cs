using Bot.Api;
using Bot.Api.Lookup;
using Bot.Core.Lookup;
using Moq;
using System;
using System.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterCaches : TestBase
    {
        private readonly Mock<IStringDownloader> m_mockDownloader = new(MockBehavior.Strict);
        private readonly Mock<ICustomScriptParser> m_mockCustomParser = new(MockBehavior.Strict);
        private readonly Mock<IDateTime> m_mockDateTime = new(MockBehavior.Strict);

        private DateTime m_currentTime = new DateTime(2022, 3, 28, 4, 5, 6);

        public TestCharacterCaches()
        {
            RegisterMock(m_mockDownloader);
            RegisterMock(m_mockCustomParser);
            RegisterMock(m_mockDateTime);

            m_mockDownloader.Setup(d => d.DownloadStringAsync(It.IsAny<string>())).ReturnsAsync(new DownloadResult(null));
            m_mockCustomParser.Setup(cp => cp.ParseScript(It.IsAny<string>())).Returns(() => new GetCustomScriptResult(Enumerable.Empty<ScriptWithCharacters>()));
            m_mockDateTime.SetupGet(dt => dt.Now).Returns(() => m_currentTime);
        }

        [Fact]
        public void CustomCache_ErrorRetieving_ReturnsNoResults()
        {
            var result = PerformCustomGet();

            Assert.Empty(result.ScriptsWithCharacters);
        }

        [Fact]
        public void CustomCache_MultipleCalls_DownloadsAndParsesOnce()
        {
            string url = "some url";
            string json = "this is some json";
            GetCustomScriptResult expectedResult = new(Enumerable.Empty<ScriptWithCharacters>());
            SetupDownload(url, json);
            SetupCustomParser(json, expectedResult);

            var csc = new CustomScriptCache(GetServiceProvider());
            var firstResult = AssertCompletedTask(() => csc.GetCustomScriptAsync(url));
            var secondResult = AssertCompletedTask(() => csc.GetCustomScriptAsync(url));
            var finalResult = AssertCompletedTask(() => csc.GetCustomScriptAsync(url));

            Assert.Equal(expectedResult, firstResult);
            Assert.Equal(expectedResult, secondResult);
            Assert.Equal(expectedResult, finalResult);
            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void CustomCache_MultipleCallsOverTime_DownloadsAndParsesAfterCacheExpires()
        {
            string url = "some url";
            string json1 = "this is some json 1";
            string json2 = "this is some json 2";
            GetCustomScriptResult expectedResult1 = new(new[] { new ScriptWithCharacters(new ScriptData("first script", isOfficial: false), Enumerable.Empty<CharacterData>()) });
            GetCustomScriptResult expectedResult2 = new(new[] { new ScriptWithCharacters(new ScriptData("second script", isOfficial: false), Enumerable.Empty<CharacterData>()) });

            m_mockDownloader.SetupSequence(d => d.DownloadStringAsync(It.Is<string>(s => s == url)))
                .ReturnsAsync(new DownloadResult(json1))
                .ReturnsAsync(new DownloadResult(json2));

            SetupCustomParser(json1, expectedResult1);
            SetupCustomParser(json2, expectedResult2);

            var csc = new CustomScriptCache(GetServiceProvider());

            var firstResult = AssertCompletedTask(() => csc.GetCustomScriptAsync(url));
            AdvanceTime(TimeSpan.FromHours(13));
            var secondResult = AssertCompletedTask(() => csc.GetCustomScriptAsync(url));

            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.IsAny<string>()), Times.Once);
            Assert.Equal(expectedResult1, firstResult);
            AdvanceTime(TimeSpan.FromHours(13));
            Assert.Equal(expectedResult1, secondResult);

            var thirdResult = AssertCompletedTask(() => csc.GetCustomScriptAsync(url));
            AdvanceTime(TimeSpan.FromHours(13));
            var fourthResult = AssertCompletedTask(() => csc.GetCustomScriptAsync(url));

            Assert.Equal(expectedResult2, thirdResult);
            AdvanceTime(TimeSpan.FromHours(13));
            Assert.Equal(expectedResult2, fourthResult);
            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void CustomCache_MultipleCallsMultipleUrls_CachesIndependently()
        {
            string url1 = "some url 1";
            string url2 = "some url 2";
            string json1a = "json 1a";
            string json1b = "json 1b";
            string json2a = "json 2a";
            string json2b = "json 2b";
            GetCustomScriptResult expected1a = new(new[] { new ScriptWithCharacters(new ScriptData("script 1a", isOfficial: false), Enumerable.Empty<CharacterData>()) });
            GetCustomScriptResult expected1b = new(new[] { new ScriptWithCharacters(new ScriptData("script 1b", isOfficial: false), Enumerable.Empty<CharacterData>()) });
            GetCustomScriptResult expected2a = new(new[] { new ScriptWithCharacters(new ScriptData("script 2a", isOfficial: false), Enumerable.Empty<CharacterData>()) });
            GetCustomScriptResult expected2b = new(new[] { new ScriptWithCharacters(new ScriptData("script 2b", isOfficial: false), Enumerable.Empty<CharacterData>()) });

            m_mockDownloader.SetupSequence(d => d.DownloadStringAsync(It.Is<string>(s => s == url1)))
                .ReturnsAsync(new DownloadResult(json1a))
                .ReturnsAsync(new DownloadResult(json1b));

            m_mockDownloader.SetupSequence(d => d.DownloadStringAsync(It.Is<string>(s => s == url2)))
                .ReturnsAsync(new DownloadResult(json2a))
                .ReturnsAsync(new DownloadResult(json2b));

            SetupCustomParser(json1a, expected1a);
            SetupCustomParser(json1b, expected1b);
            SetupCustomParser(json2a, expected2a);
            SetupCustomParser(json2b, expected2b);

            var csc = new CustomScriptCache(GetServiceProvider());

            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.Is<string>(s => s == url1)), Times.Never);

            var result1a1 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url1));
            var result1a2 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url1));

            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.Is<string>(s => s == url2)), Times.Never);
            AdvanceTime(TimeSpan.FromHours(13));

            var result2a1 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url2));
            var result2a2 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url2));

            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.Is<string>(s => s == url1)), Times.Once);
            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.Is<string>(s => s == url2)), Times.Once);

            AdvanceTime(TimeSpan.FromHours(13));

            var result1b1 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url1));
            var result1b2 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url1));

            AdvanceTime(TimeSpan.FromHours(13));

            var result2b1 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url2));
            var result2b2 = AssertCompletedTask(() => csc.GetCustomScriptAsync(url2));

            Assert.Equal(expected1a, result1a1);
            Assert.Equal(expected1a, result1a2);
            Assert.Equal(expected1b, result1b1);
            Assert.Equal(expected1b, result1b2);
            Assert.Equal(expected2a, result2a1);
            Assert.Equal(expected2a, result2a2);
            Assert.Equal(expected2b, result2b1);
            Assert.Equal(expected2b, result2b2);

            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.Is<string>(s => s == url1)), Times.Exactly(2));
            m_mockDownloader.Verify(d => d.DownloadStringAsync(It.Is<string>(s => s == url2)), Times.Exactly(2));
        }

        private GetCustomScriptResult PerformCustomGet()
        {
            var csc = new CustomScriptCache(GetServiceProvider());
            return AssertCompletedTask(() => csc.GetCustomScriptAsync("url"));
        }

        private void SetupDownload(string url, string data)
        {
            m_mockDownloader.Setup(d => d.DownloadStringAsync(It.Is<string>(s => s == url))).ReturnsAsync(new DownloadResult(data));
        }

        private void SetupDownload(string data)
        {
            m_mockDownloader.Setup(d => d.DownloadStringAsync(It.IsAny<string>())).ReturnsAsync(new DownloadResult(data));
        }

        private void SetupCustomParser(string json, GetCustomScriptResult result)
        {
            m_mockCustomParser.Setup(cp => cp.ParseScript(It.Is<string>(s => s == json))).Returns(result);
        }

        private void AdvanceTime(TimeSpan timeSpan)
        {
            m_currentTime += timeSpan;
        }
    }
}
