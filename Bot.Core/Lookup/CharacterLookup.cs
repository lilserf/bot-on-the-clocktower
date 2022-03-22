using Bot.Api.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CharacterLookup : ICharacterLookup
    {
        private readonly ICharacterStorage m_storage;

        public CharacterLookup(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_storage);
        }

        public async Task<LookupCharacterResult> LookupCharacterAsync(ulong guildId, string charString)
        {
            var officialChars = await m_storage.GetOfficialCharactersAsync();

            var matchingCharacters = FilterMatchingCharacterItems(officialChars.Items, charString);

            return new LookupCharacterResult(matchingCharacters.Select(c => new LookupCharacterItem(c.Character, c.Scripts)));
        }

        private IEnumerable<GetCharactersItem> FilterMatchingCharacterItems(IEnumerable<GetCharactersItem> characterItems, string charString)
        {
            foreach (var item in characterItems)
                if (0 == string.Compare(item.Character.Name, charString, StringComparison.InvariantCultureIgnoreCase))
                    yield return item;
        }
    }
}
