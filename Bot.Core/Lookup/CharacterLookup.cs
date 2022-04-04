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
            var charResult = await m_storage.GetCharactersAsync(guildId);
            var matchingCharacters = FilterMatchingCharacterItems(charResult.Items, charString);
            var mergedCharacters = MergeCharacterItems(matchingCharacters);

            return new LookupCharacterResult(mergedCharacters.Select(c => new LookupCharacterItem(c.Character, c.Scripts)));
        }

        private IEnumerable<GetCharactersItem> FilterMatchingCharacterItems(IEnumerable<GetCharactersItem> characterItems, string charString)
        {
            foreach (var item in characterItems)
                if (0 == string.Compare(item.Character.Name, charString, StringComparison.InvariantCultureIgnoreCase))
                    yield return item;
        }

        private IReadOnlyCollection<GetCharactersItem> MergeCharacterItems(IEnumerable<GetCharactersItem> items)
        {
            Dictionary<(string, string, CharacterTeam), (CharacterData, List<ScriptData>)> merged = new();
            List<(string, string, CharacterTeam)> order = new();

            foreach (var item in items)
                MergeCharactersItem(merged, order, item);

            return order.Select(t =>
            {
                var d = merged[t];
                return new GetCharactersItem(d.Item1, d.Item2);
            }).ToArray();


            static void MergeCharactersItem(Dictionary<(string, string, CharacterTeam), (CharacterData, List<ScriptData>)> merged, List<(string, string, CharacterTeam)> order, GetCharactersItem item)
            {
                var c = item.Character;
                var id = (Sanitize(c.Name), Sanitize(c.Ability), c.Team);

                if (!merged.TryGetValue(id, out var data))
                {
                    data = (new CharacterData(c.Id, c.Name, c.Ability, c.Team, c.IsOfficial), new List<ScriptData>());
                    merged.Add(id, data);
                    order.Add(id);
                }
                MergeCharacterItemIntoData(data, item);
            }

            static void MergeCharacterItemIntoData((CharacterData, List<ScriptData>) data, GetCharactersItem item)
            {
                data.Item1.ImageUrl ??= item.Character.ImageUrl;
                data.Item1.FlavorText ??= item.Character.FlavorText;
                data.Item2.AddRange(item.Scripts);
            }

            static string Sanitize(string s)
            {
                return new string(s.ToLowerInvariant().Where(c => !char.IsWhiteSpace(c)).ToArray());
            }
        }
    }
}
