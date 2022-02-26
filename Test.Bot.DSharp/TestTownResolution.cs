using Bot.Api;
using Bot.Api.Database;
using Bot.DSharp;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.DSharp
{
    public class TestTownResolution : TestBase
    {
        [Fact(Skip="Working on creating mock for DiscordClient")]
        public void ResolveTown_ResolvesCategoryChannels()
        {
            var mockEnv = RegisterMock(new Mock<IEnvironment>());
            mockEnv.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns("some string");

            const ulong guildId = 123ul;
            var mockGuild = new Mock<IGuild>();

            //var mock

            Mock<ITownRecord> mockTownRecord = new();
            mockTownRecord.Setup(tr => tr.GuildId).Returns(guildId);

            var dsc = new DSharpClient(GetServiceProvider());

            var t = dsc.ResolveTownAsync(mockTownRecord.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            throw new NotImplementedException();
        }
    }
}
