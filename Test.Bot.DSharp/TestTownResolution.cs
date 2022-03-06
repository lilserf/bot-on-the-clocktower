using Bot.Api;
using Bot.Api.Database;
using Bot.DSharp;
using Bot.DSharp.DiscordWrappers;
using DSharpPlus;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.DSharp
{
    public class TestTownResolution : TestBase
    {
        [Fact]
        public void ResolveTown_ResolvesCategoryChannels()
        {
            var envMock = RegisterMock(new Mock<IEnvironment>());
            envMock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns("some string");

            const ulong guildId = 123ul;
            var guildMock = new Mock<IDiscordGuild>(MockBehavior.Strict);

            const ulong dayCatId = 10ul;
            const ulong nightCatId = 20ul;
            const ulong townSquareId = 101ul;
            const ulong darkAlleyId = 102ul;
            const ulong cottage1Id = 201ul;
            const ulong cottage2Id = 202ul;

            ChannelMockHolder townSquareMocks = new(townSquareId, "Town Square");
            ChannelMockHolder darkAlleyMocks = new(darkAlleyId, "Dark Alley");
            ChannelMockHolder cottage1Mocks = new(cottage1Id, "Cottage");
            ChannelMockHolder cottage2Mocks = new(cottage2Id, "Cottage");

            Mock<IDiscordChannelCategory> dayCatMock = new(MockBehavior.Strict);
            dayCatMock.SetupGet(c => c.Id).Returns(dayCatId);
            dayCatMock.SetupGet(c => c.Channels).Returns(new[] { townSquareMocks.Unresolved.Object, darkAlleyMocks.Unresolved.Object });

            Mock<IDiscordChannelCategory> nightCatMock = new(MockBehavior.Strict);
            nightCatMock.SetupGet(c => c.Id).Returns(nightCatId);
            nightCatMock.SetupGet(c => c.Channels).Returns(new[] { cottage1Mocks.Unresolved.Object, cottage2Mocks.Unresolved.Object });

            Mock<ITownRecord> mockTownRecord = new();
            mockTownRecord.Setup(tr => tr.GuildId).Returns(guildId);
            mockTownRecord.Setup(tr => tr.DayCategoryId).Returns(dayCatId);
            mockTownRecord.Setup(tr => tr.NightCategoryId).Returns(nightCatId);

            Mock<IDiscordClient> mockDiscordClient = new(MockBehavior.Strict);
            mockDiscordClient.Setup(dc => dc.GetGuildAsync(It.Is<ulong>(l => l == guildId))).ReturnsAsync(guildMock.Object);
            SetupGetChannelCategory(mockDiscordClient, dayCatId, dayCatMock);
            SetupGetChannelCategory(mockDiscordClient, nightCatId, nightCatMock);
            SetupGetChannel(mockDiscordClient, townSquareId, townSquareMocks.Resolved);
            SetupGetChannel(mockDiscordClient, darkAlleyId, darkAlleyMocks.Resolved);
            SetupGetChannel(mockDiscordClient, cottage1Id, cottage1Mocks.Resolved);
            SetupGetChannel(mockDiscordClient, cottage2Id, cottage2Mocks.Resolved);
            static void SetupGetChannelCategory(Mock<IDiscordClient> mockClient, ulong id, Mock<IDiscordChannelCategory> mockChannelCat) =>
                mockClient.Setup(dc => dc.GetChannelCategoryAsync(It.Is<ulong>(l => l == id))).ReturnsAsync(mockChannelCat.Object);
            static void SetupGetChannel(Mock<IDiscordClient> mockClient, ulong id, Mock<IDiscordChannel> mockChannel) =>
                mockClient.Setup(dc => dc.GetChannelAsync(It.Is<ulong>(l => l == id))).ReturnsAsync(mockChannel.Object);

            var mockDiscordClientFactory = RegisterMock(new Mock<IDiscordClientFactory>(MockBehavior.Strict));
            mockDiscordClientFactory.Setup(dcf => dcf.CreateClient(It.IsAny<DiscordConfiguration>())).Returns(mockDiscordClient.Object);

            var dsc = new DSharpClient(GetServiceProvider());

            var t = dsc.ResolveTownAsync(mockTownRecord.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            throw new NotImplementedException();
        }

        private class ChannelMockHolder
        {
            public Mock<IDiscordChannel> Unresolved { get; }
            public Mock<IDiscordChannel> Resolved { get; }
            public ChannelMockHolder(ulong id, string name, params IMember[] members)
            {
                Unresolved = new(MockBehavior.Strict);
                Unresolved.SetupGet(c => c.Id).Returns(id);

                Resolved = new(MockBehavior.Strict);
                Resolved.SetupGet(c => c.Id).Returns(id);
                Resolved.SetupGet(c => c.Name).Returns(name);
                Resolved.SetupGet(c => c.Users).Returns(members);
            }
        }
    }
}
