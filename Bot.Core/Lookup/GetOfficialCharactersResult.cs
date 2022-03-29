using Bot.Api.Lookup;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Core.Lookup
{
    public class GetOfficialCharactersResult
    {
        public IReadOnlyCollection<GetOfficialCharactersItem> Items { get; }

        public GetOfficialCharactersResult(IEnumerable<GetOfficialCharactersItem> items)
        {
            Items = items.ToArray();
        }
    }

    public class GetOfficialCharactersItem
    {
        public CharacterData Character { get; }
        public IReadOnlyCollection<ScriptData> Scripts { get; }
        public GetOfficialCharactersItem(CharacterData character, IEnumerable<ScriptData> scripts)
        {
            Character = character;
            Scripts = scripts.ToArray();
        }
    }
}
