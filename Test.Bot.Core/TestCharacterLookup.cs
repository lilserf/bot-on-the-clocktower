using Bot.Api.Lookup;
using Bot.Core.Lookup;
using Moq;
using System.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCharacterLookup : TestBase
    {
        private const ulong GuildId = 123ul;

        private readonly CharacterData m_officialWasherWoman;
        private readonly CharacterData m_officialPoisoner;
        private readonly ScriptData m_troubleBrewing;

        private readonly Mock<ICharacterStorage> m_mockCharacterStorage = new();

        //private readonly string m_tbJson = "{ \"id\": \"washerwoman\", \"name\": \"Washerwoman\", \"edition\": \"tb\", \"team\": \"townsfolk\", \"firstNight\": 32, \"firstNightReminder\": \"Show the character token of a Townsfolk in play. Point to two players, one of which is that character.\", \"otherNight\": 0, \"otherNightReminder\": \"\", \"reminders\": [\"Townsfolk\", \"Wrong\"], \"setup\": false, \"ability\": \"You start knowing that 1 of 2 players is a particular Townsfolk.\" }, { \"id\": \"poisoner\", \"name\": \"Poisoner\", \"edition\": \"tb\", \"team\": \"minion\", \"firstNight\": 17, \"firstNightReminder\": \"The Poisoner points to a player. That player is poisoned.\", \"otherNight\": 8, \"otherNightReminder\": \"The previously poisoned player is no longer poisoned. The Poisoner points to a player. That player is poisoned.\", \"reminders\": [\"Poisoned\"], \"setup\": false, \"ability\": \"Each night, choose a player: they are poisoned tonight and tomorrow day.\" }";

        public TestCharacterLookup()
        {
            m_officialWasherWoman = new CharacterData("Washerwoman", "You start knowing that 1 or 2 players is a particular Townsfolk", CharacterTeam.Townsfolk, isOfficial: true);
            m_officialPoisoner = new CharacterData("Poisoner", "Each night, choose a player: they are poisoned tonight and tomorrow day.", CharacterTeam.Minion, isOfficial: true);
            m_troubleBrewing = new ScriptData("Trouble Brewing", true);

            RegisterMock(m_mockCharacterStorage);
            m_mockCharacterStorage.Setup(s => s.GetOfficialCharactersAsync()).ReturnsAsync(
                new GetCharactersResult(new[] {
                    new GetCharactersItem(m_officialWasherWoman, new[] { m_troubleBrewing }),
                    new GetCharactersItem(m_officialPoisoner, new[] { m_troubleBrewing }),
                }));
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

            var result = AssertCompletedTask(() => lookup.LookupCharacterAsync(GuildId, "Washerwoman"));

            Assert.Equal(1, result.Items.Count);
            Assert.Equal(result.Items.First().Character, m_officialWasherWoman);
            Assert.Equal(1, result.Items.First().Scripts.Count);
            Assert.Equal(result.Items.First().Scripts.First(), m_troubleBrewing);
        }
    }
}
