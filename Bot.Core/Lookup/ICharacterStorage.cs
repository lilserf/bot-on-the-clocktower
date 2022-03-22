using Bot.Api.Lookup;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface ICharacterStorage
    {
        public Task<GetCharactersResult> GetOfficialScriptCharactersAsync();
        public Task<GetCharactersResult> GetCustomScriptCharactersAsync(ulong guildId);
    }

    public class GetCharactersResult
    {
        public IReadOnlyCollection<GetCharactersItem> Items { get; }
        public GetCharactersResult(IEnumerable<GetCharactersItem> items)
        {
            Items = items.ToArray();
        }
    }

    public class GetCharactersItem
    {
        public CharacterData Character { get; }
        public IReadOnlyCollection<ScriptData> Scripts { get; }

        public GetCharactersItem(CharacterData character, IEnumerable<ScriptData> scripts)
        {
            Character = character;
            Scripts = scripts.ToArray();
        }
    }
}
