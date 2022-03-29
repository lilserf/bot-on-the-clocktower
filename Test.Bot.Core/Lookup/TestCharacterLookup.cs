using Bot.Api.Lookup;
using Bot.Core.Lookup;
using Moq;
using System.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestCharacterLookup : TestBase
    {
        private const ulong GuildId = 123ul;

        private readonly CharacterData m_officialWasherWoman;
        private readonly CharacterData m_officialImp;
        private readonly CharacterData m_officialPoisoner;
        private readonly ScriptData m_troubleBrewing;

        private readonly CharacterData m_officialCultLeader;

        private readonly CharacterData m_customIcarus;
        private readonly CharacterData m_customImp;
        private readonly CharacterData m_customCultLeader;
        private readonly ScriptData m_customScript1;
        private readonly ScriptData m_customScript2;
        private readonly ScriptData m_customScript3;

        private readonly Mock<ICharacterStorage> m_mockCharacterStorage = new();

        public TestCharacterLookup()
        {
            m_officialWasherWoman = new("Washerwoman", "You start knowing that 1 or 2 players is a particular Townsfolk.", CharacterTeam.Townsfolk, isOfficial: true);
            m_officialImp = new("Imp", "Each night*, choose a player: they die. If you kill yourself this way, a Minion becomes the Imp.", CharacterTeam.Demon, isOfficial: true);
            m_officialImp.ImageUrl = "imp image url";
            m_officialPoisoner = new("Poisoner", "Each night, choose a player: they are poisoned tonight and tomorrow day.", CharacterTeam.Minion, isOfficial: true);
            m_troubleBrewing = new("Trouble Brewing", true);

            m_officialCultLeader = new("Cult Leader", "Each night, you become the alignment of an alive neighbour. If all good players choose to join your cult, your team wins.", CharacterTeam.Townsfolk, isOfficial: true);
            m_customCultLeader = new(m_officialCultLeader.Name, m_officialCultLeader.Ability, CharacterTeam.Outsider, isOfficial: false);

            m_customIcarus = new("Icarus", "Each day you may privately ask the storyteller a question. Either they answer truthfully or you become drunk for the rest of the game.", CharacterTeam.Townsfolk, isOfficial: false);
            m_customIcarus.FlavorText = "Let me warn you, Icarus, take the middle way, because the moisture will weigh down your wings, if you fly too low. But if you go too high, the sun will scorch them. Travel between the extremes and take the course I’ll show you!";
            m_customImp = new(m_officialImp.Name, m_officialImp.Ability, m_officialImp.Team, isOfficial: false);
            m_customImp.FlavorText = "We must keep our wits sharp and our sword sharper. Evil walks among us, and will stop at nothing to destroy us good, simple folk, bringing our fine town to ruin. Trust no-one. But, if you must trust someone, trust me.";

            m_customScript1 = new("Custom Script 1", false);
            m_customScript2 = new("Custom Script 2", false);
            m_customScript3 = new("Custom Script 3", false);

            m_mockCharacterStorage.Setup(s => s.GetCharactersAsync(It.Is<ulong>(id => id == GuildId))).ReturnsAsync(
                new GetCharactersResult(new[] {
                    new GetCharactersItem(m_officialWasherWoman, new[] { m_troubleBrewing }),
                    new GetCharactersItem(m_officialPoisoner, new[] { m_troubleBrewing }),
                    new GetCharactersItem(m_officialImp, new[] { m_troubleBrewing }),
                    new GetCharactersItem(m_officialCultLeader, Enumerable.Empty<ScriptData>()),
                    new GetCharactersItem(m_officialPoisoner, new[] { m_customScript1 }),
                    new GetCharactersItem(m_customIcarus, new[] { m_customScript1, m_customScript2 }),
                    new GetCharactersItem(m_customImp, new[] { m_customScript2 }),
                    new GetCharactersItem(m_officialCultLeader, new[] { m_customScript2 }),
                    new GetCharactersItem(m_customCultLeader, new[] { m_customScript3 }),
                }));

            RegisterMock(m_mockCharacterStorage);
        }

        [Fact]
        public void CharacterLookup_BogusRequest_NoResults()
        {
            var lookup = new CharacterLookup(GetServiceProvider());

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "this is a bogus character request"));

            Assert.Empty(result.Items);
        }

        [Fact]
        public void AnyGuild_LegitRequest_ReturnsResult()
        {
            var lookup = new CharacterLookup(GetServiceProvider());

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "washerwoman"));

            Assert.Collection(result.Items,
                i =>
                {
                    AssertEquivalentRole(m_officialWasherWoman, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(s, m_troubleBrewing));
                });
        }

        [Fact]
        public void Guild1_OfficialRoleInCustomScript_ReturnsBothScriptsOneItem()
        {
            var lookup = new CharacterLookup(GetServiceProvider());

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "poisoner"));

            Assert.Collection(result.Items,
                i =>
                {
                    AssertEquivalentRole(m_officialPoisoner, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(s, m_troubleBrewing),
                        s => Assert.Equal(s, m_customScript1));
                });
        }

        [Fact]
        public void Guild1_CustomRoleInCustomScript2_ReturnsBothScriptsOneItem()
        {
            var lookup = new CharacterLookup(GetServiceProvider());

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "icarus"));

            Assert.Collection(result.Items,
                i =>
                {
                    AssertEquivalentRole(m_customIcarus, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(s, m_customScript1),
                        s => Assert.Equal(s, m_customScript2));
                });
        }

        [Fact]
        public void Guild1_OfficialRoleTweakedInCustomScript_ReturnsTwoItems()
        {
            var lookup = new CharacterLookup(GetServiceProvider());

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "cult leader"));

            Assert.Collection(result.Items,
                i =>
                {
                    AssertEquivalentRole(m_officialCultLeader, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(s, m_customScript2));
                },
                i =>
                {
                    AssertEquivalentRole(m_customCultLeader, i.Character);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(s, m_customScript3));
                });
        }

        [Fact]
        public void Guild1_OfficialRoleWithCustomFlavorProvided_ReturnsWithFlavor()
        {
            var lookup = new CharacterLookup(GetServiceProvider());

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "imp"));

            Assert.Collection(result.Items,
                i =>
                {
                    AssertEquivalentRole(m_officialImp, i.Character);
                    Assert.Equal(m_officialImp.ImageUrl, i.Character.ImageUrl);
                    Assert.Equal(m_customImp.FlavorText, i.Character.FlavorText);
                    Assert.Collection(i.Scripts,
                        s => Assert.Equal(s, m_troubleBrewing),
                        s => Assert.Equal(s, m_customScript2));
                });
        }

        private static void AssertEquivalentRole(CharacterData a, CharacterData b)
        {
            Assert.Equal(a.Name, b.Name);
            Assert.Equal(a.Ability, b.Ability);
            Assert.Equal(a.Team, b.Team);
            Assert.Equal(a.IsOfficial, b.IsOfficial);
        }
    }
}
