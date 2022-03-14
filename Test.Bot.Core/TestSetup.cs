using Bot.Api;
using Bot.Api.Database;
using Bot.Core;
using Moq;
using System.Drawing;
using System.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestSetup : TestBase
    {
        const string TownName = "Clockburg";
        const string TownSquareName = "Town Square";
        const string DayCatName = "Clockburg - Day";
        const string NightCatName = "Clockburg - Night";
        const string ControlChannelName = "Clockburg Control";
        const string ChatChannelName = "Clockburg Chat";
        const string StorytellerName = "Clockburg Storyteller";
        const string VillagerName = "Clockburg Villager";

        const string TownAuthor = "Town Author";
        readonly ulong MockGuildId = 1337;

        private readonly Mock<ITown> TownMock;
        private readonly Mock<IMember> AuthorMock;
        private readonly Mock<IGuild> GuildMock;
        private readonly Mock<ITownDatabase> TownDatabaseMock;
        private readonly Mock<IBotSystem> SystemMock;
        private TownDescription TownDesc;

        public TestSetup()
        {
            TownMock = MakeTown();
            AuthorMock = MakeAuthor();
            GuildMock = MakeGuild();

            TownDesc = MakeDesc(GuildMock.Object, AuthorMock.Object);

            TownDatabaseMock = new();
            TownDatabaseMock.Setup(x => x.AddTown(It.IsAny<ITown>(), It.IsAny<IMember>()));
            RegisterMock(TownDatabaseMock);

            SystemMock = new();
            SystemMock.Setup(x => x.CreateTextInput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>())).Returns(new Mock<IBotComponent>().Object);
            RegisterMock(SystemMock);
        }

        private static Mock<IChannel> MakeChannel(string name)
        {
            Mock<IChannel> chan = new() { Name = name };
            chan.SetupGet(x => x.Name).Returns(name);
            chan.SetupGet(x => x.Id).Returns((ulong)name.GetHashCode());
            return chan;
        }

        private static Mock<IChannelCategory> MakeChannelCategory(string name)
        {
            Mock<IChannelCategory> cat = new() { Name = name };
            cat.SetupGet(x => x.Name).Returns(name);
            cat.SetupGet(x => x.Id).Returns((ulong)name.GetHashCode());
            return cat;
        }

        private static Mock<IRole> MakeRole(string name)
        {
            Mock<IRole> role = new() { Name = name };
            role.SetupGet(x => x.Name).Returns(name);
            role.SetupGet(x => x.Id).Returns((ulong)name.GetHashCode());
            return role;
        }

        public static Mock<ITown> MakeTown()
        {
            Mock<ITown> town = new();
            town.SetupGet(x => x.DayCategory).Returns(MakeChannelCategory(DayCatName).Object);
            town.SetupGet(x => x.NightCategory).Returns(MakeChannelCategory(NightCatName).Object);
            town.SetupGet(x => x.TownSquare).Returns(MakeChannel(TownSquareName).Object);
            town.SetupGet(x => x.ControlChannel).Returns(MakeChannel(ControlChannelName).Object);
            town.SetupGet(x => x.ChatChannel).Returns(MakeChannel(ChatChannelName).Object);
            town.SetupGet(x => x.StorytellerRole).Returns(MakeRole(StorytellerName).Object);
            town.SetupGet(x => x.VillagerRole).Returns(MakeRole(VillagerName).Object);
            return town;
        }

        public static Mock<IMember> MakeAuthor()
        { 
            Mock<IMember> author = new();
            author.SetupGet(x => x.DisplayName).Returns(TownAuthor);
            author.SetupGet(x => x.Id).Returns((ulong)TownAuthor.GetHashCode());
            return author;
        }

        public Mock<IGuild> MakeGuild()
        {
            Mock<IGuild> guild = new();
            guild.SetupGet(x => x.Id).Returns(MockGuildId);
            guild.Setup(x => x.CreateTextChannelAsync(It.IsAny<string>(), It.IsAny<IChannel?>())); // TODO: make this actually do something?
            guild.Setup(x => x.CreateVoiceChannelAsync(It.IsAny<string>(), It.IsAny<IChannel?>())); // TODO: make this actually do something?
            guild.Setup(x => x.CreateCategoryAsync(It.IsAny<string>())); // TODO: make this actually do something?
            guild.Setup(x => x.CreateRoleAsync(It.IsAny<string>(), It.IsAny<Color>())); // TODO: make this actually do something?
            return guild;
        }

        public static TownDescription MakeDesc(IGuild guild, IMember author)
        {
            TownDescription desc = new()
            {
                Guild = guild,
                TownName = TownName,
                DayCategoryName = DayCatName,
                NightCategoryName = NightCatName,
                TownSquareName = TownSquareName,
                ControlChannelName = ControlChannelName,
                ChatChannelName = ChatChannelName,
                StorytellerRoleName = StorytellerName,
                VillagerRoleName = VillagerName,
                Author = author,
            };

            return desc;
        }

        [Fact]
        public void AddTown_CallsTownDb()
        {
            ITown town = TownMock.Object;
            IMember author = AuthorMock.Object;

            Mock<ITownDatabase> townDb = new();
            townDb.Setup(x => x.AddTown(It.IsAny<ITown>(), It.IsAny<IMember>()));
            RegisterMock(townDb);

            BotSetup bs = new(GetServiceProvider());
            var t = bs.AddTown(town, author);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            townDb.Verify(x => x.AddTown(It.Is<ITown>(y => y == town), It.Is<IMember>(z => z == author)));
        }

        private BotSetup CreateTownAssertCompleted()
        {
            BotSetup bs = new(GetServiceProvider());
            var t = bs.CreateTown(TownDesc);
            t.Wait(50);
            Assert.True(t.IsCompleted);
            return bs;
        }

        [Fact(Skip ="Can't test this until the rest of it is in place, adding to the DB is last")]
        public void CreateTown_CallsTownDb()
        {
            CreateTownAssertCompleted();

            TownDatabaseMock.Verify(x => x.AddTown(
                It.Is<ITown>(y => y.DayCategory!.Id == (ulong)DayCatName.GetHashCode() &&
                                  y.NightCategory!.Id == (ulong)NightCatName.GetHashCode() &&
                                  y.ControlChannel!.Id == (ulong)ControlChannelName.GetHashCode() &&
                                  y.ChatChannel!.Id == (ulong)ChatChannelName.GetHashCode() &&
                                  y.TownSquare!.Id == (ulong)TownSquareName.GetHashCode() &&
                                  y.StorytellerRole!.Id == (ulong)StorytellerName.GetHashCode() &&
                                  y.VillagerRole!.Id == (ulong)VillagerName.GetHashCode()), 
                It.Is<IMember>(z => z == AuthorMock.Object)));

        }

        private void VerifyRequiredRoles(IBotSetup _)
        {
            GuildMock.Verify(x => x.CreateRoleAsync(It.Is<string>(s => s == StorytellerName), It.IsAny<Color>()), Times.Once);
            GuildMock.Verify(x => x.CreateRoleAsync(It.Is<string>(s => s == VillagerName), It.IsAny<Color>()), Times.Once);
        }

        private void VerifyRequiredChannels(IBotSetup bs)
        {
            GuildMock.Verify(x => x.CreateCategoryAsync(It.Is<string>(s => s == DayCatName)), Times.Once);
            // TODO: how to make sure the calls happen with the correct parent channel
            GuildMock.Verify(x => x.CreateVoiceChannelAsync(It.Is<string>(s => s == TownSquareName), It.IsAny<IChannel?>()), Times.Once);
            foreach (var name in bs.DefaultExtraDayChannels)
                GuildMock.Verify(x => x.CreateVoiceChannelAsync(It.Is<string>(s => s == name), It.IsAny<IChannel?>()), Times.Once);
            GuildMock.Verify(x => x.CreateTextChannelAsync(It.Is<string>(s => s == ControlChannelName), It.IsAny<IChannel?>()), Times.Once);
        }

        private void VerifyOptionalChannels(IBotSetup _)
        {
            GuildMock.Verify(x => x.CreateTextChannelAsync(It.Is<string>(s => s == ChatChannelName), It.IsAny<IChannel?>()), Times.Once);
            GuildMock.Verify(x => x.CreateCategoryAsync(It.Is<string>(s => s == NightCatName)), Times.Once);
            GuildMock.Verify(x => x.CreateVoiceChannelAsync(It.Is<string>(s => s == IBotSetup.DefaultCottageName), It.IsAny<IChannel?>()), Times.Exactly(20));
        }

        [Fact]
        public void CreateTown_FullDesc_ChannelsAndRolesCreated()
        {
            var bs = CreateTownAssertCompleted();

            VerifyRequiredChannels(bs);
            VerifyOptionalChannels(bs);
            VerifyRequiredRoles(bs);
        }


        [Fact]
        public void CreateTown_NoNight()
        {
            TownDesc.NightCategoryName = null;

            var bs = CreateTownAssertCompleted();

            VerifyRequiredChannels(bs);
            VerifyRequiredRoles(bs);
            // Make sure night stuff DIDN'T happen
            GuildMock.Verify(x => x.CreateCategoryAsync(It.Is<string>(s => s == NightCatName)), Times.Never);
            GuildMock.Verify(x => x.CreateVoiceChannelAsync(It.Is<string>(s => s == IBotSetup.DefaultCottageName), It.IsAny<IChannel?>()), Times.Never);
        }

        [Fact]
        public void CreateTown_TestDefaults()
        {
            // Set all the stuff that has a default to null and see if we get the defaults
            TownDesc.DayCategoryName = null;
            TownDesc.ControlChannelName = null;
            TownDesc.ChatChannelName = null;
            TownDesc.TownSquareName = null;
            TownDesc.StorytellerRoleName = null;
            TownDesc.VillagerRoleName = null;

            var bs = CreateTownAssertCompleted();

            // Check for default channel names
            var dayCatName = string.Format(IBotSetup.DefaultDayCategoryFormat, TownDesc.TownName);
            GuildMock.Verify(x => x.CreateCategoryAsync(It.Is<string>(s => s == dayCatName)), Times.Once);
            // TODO: how to make sure the calls happen with the correct parent channel
            var tsName = IBotSetup.DefaultTownSquareChannelName;
            GuildMock.Verify(x => x.CreateVoiceChannelAsync(It.Is<string>(s => s == tsName), It.IsAny<IChannel?>()), Times.Once);
            foreach (var name in bs.DefaultExtraDayChannels)
                GuildMock.Verify(x => x.CreateVoiceChannelAsync(It.Is<string>(s => s == name), It.IsAny<IChannel?>()), Times.Once);
            var ctrlName = string.Format(IBotSetup.DefaultControlChannelFormat, TownDesc.TownName);
            GuildMock.Verify(x => x.CreateTextChannelAsync(It.Is<string>(s => s == ctrlName), It.IsAny<IChannel?>()), Times.Once);

            // Chat should not have been created
            GuildMock.Verify(x => x.CreateTextChannelAsync(It.Is<string>(s => s == ChatChannelName), It.IsAny<IChannel?>()), Times.Never);

            // Might as well confirm Night still worked
            GuildMock.Verify(x => x.CreateCategoryAsync(It.Is<string>(s => s == NightCatName)), Times.Once);
            GuildMock.Verify(x => x.CreateVoiceChannelAsync(It.Is<string>(s => s == IBotSetup.DefaultCottageName), It.IsAny<IChannel?>()), Times.Exactly(20));

            // Check for default role names
            var stName = string.Format(IBotSetup.DefaultStorytellerRoleFormat, TownDesc.TownName);
            GuildMock.Verify(x => x.CreateRoleAsync(It.Is<string>(s => s == stName), It.IsAny<Color>()), Times.Once);
            var villagerName = string.Format(IBotSetup.DefaultVillagerRoleFormat, TownDesc.TownName);
            GuildMock.Verify(x => x.CreateRoleAsync(It.Is<string>(s => s == villagerName), It.IsAny<Color>()), Times.Once);
        }
        // TODO:
        // - leave off required bits and get exceptions
    }
}
