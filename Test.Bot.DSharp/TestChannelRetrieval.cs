using Bot.Api;
using Bot.Api.Database;
using Bot.Core;
using Bot.DSharp;
using DSharpPlus;
using Moq;
using System;
using System.Collections.Generic;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.DSharp
{
    public class TestChannelRetrieval : TestBase
    {
        private readonly Mock<IBotClient> m_mockClient = new(MockBehavior.Strict);

        private readonly Mock<IChannel> m_mockControlChannel = new(MockBehavior.Strict);
        private readonly Mock<IChannel> m_mockTownSquareChannel = new(MockBehavior.Strict);
        private readonly Mock<IChannel> m_mockChatChannel = new(MockBehavior.Strict);
        private readonly Mock<IChannelCategory> m_mockDayChannelCategory = new(MockBehavior.Strict);
        private readonly Mock<IChannelCategory> m_mockNightChannelCategory = new(MockBehavior.Strict);
        private readonly Mock<ITownRecord> m_mockTownRecord = new(MockBehavior.Strict);
        private readonly Mock<ITownDatabase> m_mockTownDb = new(MockBehavior.Strict);
        private readonly Mock<IGuild> m_mockGuild = new(MockBehavior.Strict);
        private readonly Mock<IRole> m_mockStorytellerRole = new(MockBehavior.Strict);
        private readonly Mock<IRole> m_mockVillagerRole = new(MockBehavior.Strict);

        private const string MismatchedName = "mismatched name";
        private const string ControlName = "control chan";
        private const string TownSquareName = "TS chan";
        private const string ChatName = "chat chan";
        private const string DayCategoryName = "day cat";
        private const string NightCategoryName = "night cat";

        private const ulong MismatchedId = 0;
        private const ulong ControlId = 1;
        private const ulong TownSquareId = 2;
        private const ulong ChatId = 3;
        private const ulong DayCategoryId = 4;
        private const ulong NightCategoryId = 5;
        private const ulong StorytellerRoleId = 6;
        private const ulong VillagerRoleId = 7;
        private const ulong GuildId = 77;

        public TestChannelRetrieval()
        {
            RegisterMock(m_mockClient);
            RegisterMock(m_mockTownDb);

            var env = RegisterMock(new Mock<IEnvironment>());
            env.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns("env var");

            SetupChannelMock(m_mockClient, m_mockControlChannel, ControlId, ControlName, false);
            SetupChannelMock(m_mockClient, m_mockTownSquareChannel, TownSquareId, TownSquareName, true);
            SetupChannelMock(m_mockClient, m_mockChatChannel, ChatId, ChatName, false);
            SetupChannelCategoryMock(m_mockClient, m_mockDayChannelCategory, DayCategoryId, DayCategoryName);
            SetupChannelCategoryMock(m_mockClient, m_mockNightChannelCategory, NightCategoryId, NightCategoryName);

            m_mockTownRecord.SetupGet(tr => tr.ControlChannel).Returns(ControlName);
            m_mockTownRecord.SetupGet(tr => tr.ControlChannelId).Returns(ControlId);
            m_mockTownRecord.SetupGet(tr => tr.TownSquare).Returns(TownSquareName);
            m_mockTownRecord.SetupGet(tr => tr.TownSquareId).Returns(TownSquareId);
            m_mockTownRecord.SetupGet(tr => tr.ChatChannel).Returns(ChatName);
            m_mockTownRecord.SetupGet(tr => tr.ChatChannelId).Returns(ChatId);
            m_mockTownRecord.SetupGet(tr => tr.DayCategory).Returns(DayCategoryName);
            m_mockTownRecord.SetupGet(tr => tr.DayCategoryId).Returns(DayCategoryId);
            m_mockTownRecord.SetupGet(tr => tr.NightCategory).Returns(NightCategoryName);
            m_mockTownRecord.SetupGet(tr => tr.NightCategoryId).Returns(NightCategoryId);
            m_mockTownRecord.SetupGet(tr => tr.StorytellerRoleId).Returns(StorytellerRoleId);
            m_mockTownRecord.SetupGet(tr => tr.VillagerRoleId).Returns(VillagerRoleId);
            m_mockTownRecord.SetupGet(tr => tr.GuildId).Returns(GuildId);

            static void SetupChannelMock(Mock<IBotClient> clientMock, Mock<IChannel> channelMock, ulong channelId, string channelName, bool expectedVoice)
            {
                channelMock.SetupGet(c => c.Id).Returns(channelId);
                channelMock.SetupGet(c => c.Name).Returns(channelName);
                channelMock.SetupGet(c => c.IsVoice).Returns(expectedVoice);
                clientMock.Setup(c => c.GetChannelAsync(It.Is<ulong>(id => id == channelId))).ReturnsAsync(channelMock.Object);
            }

            static void SetupChannelCategoryMock(Mock<IBotClient> clientMock, Mock<IChannelCategory> channelMock, ulong channelId, string channelName)
            {
                channelMock.SetupGet(c => c.Id).Returns(channelId);
                channelMock.SetupGet(c => c.Name).Returns(channelName);
                clientMock.Setup(c => c.GetChannelCategoryAsync(It.Is<ulong>(id => id == channelId))).ReturnsAsync(channelMock.Object);
            }

            m_mockClient.Setup(c => c.GetGuildAsync(It.Is<ulong>(id => id == GuildId))).ReturnsAsync(m_mockGuild.Object);

            Dictionary<ulong, IRole> roleDict = new()
            {
                { StorytellerRoleId, m_mockStorytellerRole.Object },
                { VillagerRoleId, m_mockVillagerRole.Object },
            };
            m_mockGuild.SetupGet(g => g.Id).Returns(GuildId);
            m_mockGuild.SetupGet(g => g.Roles).Returns(roleDict);

            m_mockTownDb.Setup(db => db.UpdateTownAsync(It.IsAny<ITown>())).ReturnsAsync(true);
        }

        [Fact]
        public void TownResolve_AllCorrect_NoRequestsUpdate()
        {
            TestResolve_VerifyTownNotUpdated();
        }

        [Fact]
        public void TownResolveChatNameOff_RequestsUpdate()
        {
            m_mockChatChannel.SetupGet(c => c.Name).Returns(MismatchedName);
            TestResolve_VerifyTownUpdated();
        }

        private void TestResolve_VerifyTownUpdated() => TestResolve_VerifyTownUpdatedTimes(Times.Once());
        private void TestResolve_VerifyTownNotUpdated() => TestResolve_VerifyTownUpdatedTimes(Times.Never());

        private void TestResolve_VerifyTownUpdatedTimes(Times numTimes)
        {
            var tr = new TownResolver(GetServiceProvider());
            var resolveTask = tr.ResolveTownAsync(m_mockTownRecord.Object);
            resolveTask.Wait(50);
            Assert.True(resolveTask.IsCompleted);

            m_mockTownDb.Verify(db => db.UpdateTownAsync(It.Is<ITown>(t => UpdatedTownMatches(t))), numTimes);
        }

        private bool UpdatedTownMatches(ITown update)
        {
            return
                update.Guild == m_mockGuild.Object &&
                update.TownSquare == m_mockTownSquareChannel.Object &&
                update.ChatChannel == m_mockChatChannel.Object &&
                update.ControlChannel == m_mockControlChannel.Object &&
                update.DayCategory == m_mockDayChannelCategory.Object &&
                update.NightCategory == m_mockNightChannelCategory.Object &&
                update.StorytellerRole == m_mockStorytellerRole.Object &&
                update.VillagerRole == m_mockVillagerRole.Object;
        }
    }
}
