using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CharacterStorage : ICharacterStorage
    {
        public Task<GetCharactersResult> GetOfficialScriptCharactersAsync()
        {
            throw new System.NotImplementedException();
        }
        public Task<GetCharactersResult> GetCustomScriptCharactersAsync(ulong guildId)
        {
            throw new System.NotImplementedException();
        }
    }
}
