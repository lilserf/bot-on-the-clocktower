using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Api.Lookup;

namespace Bot.Core.Lookup
{
    public interface IOfficialCharacterCache
    {
        public Task<GetOfficialCharactersResult> GetOfficialCharactersAsync();
    }

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
