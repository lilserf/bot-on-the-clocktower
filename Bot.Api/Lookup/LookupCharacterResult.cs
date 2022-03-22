using System.Collections.Generic;
using System.Linq;

namespace Bot.Api.Lookup
{
    public class LookupCharacterResult
    {
        public IReadOnlyCollection<LookupCharacterItem> Items { get; }

        public LookupCharacterResult(IEnumerable<LookupCharacterItem> items)
        {
            Items = items.ToArray();
        }
    }

    public class LookupCharacterItem
    {
        public CharacterData Character { get; }
        public IReadOnlyCollection<ScriptData> Scripts { get; }

        public LookupCharacterItem(CharacterData character, IEnumerable<ScriptData> scripts)
        {
            Character = character;
            Scripts = scripts.ToArray();
        }
    }
}
