using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface ICharacterStorage
    {
        Task<GetCharactersResult> GetCharactersAsync(ulong guildId);
        Task RefreshCharactersAsync(ulong guildId);
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
