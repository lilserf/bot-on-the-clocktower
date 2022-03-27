using System;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CharacterStorage : ICharacterStorage
    {
        private readonly IScriptCache m_scriptCache;

        public CharacterStorage(IServiceProvider serviceProvider)
        {
            //serviceProvider.Inject(out m_scriptCache);
        }

        public async Task<GetCharactersResult> GetOfficialScriptCharactersAsync()
        {
            throw new System.NotImplementedException();
            //var official = await m_scriptCache.GetOfficialScriptsAsync();

            //foreach (var char in official)
            // TODO Create items for each character (no merging! that's handled elsewhere!)

            //return new GetCharactersResult()
        }
        public Task<GetCharactersResult> GetCustomScriptCharactersAsync(ulong guildId)
        {
            throw new System.NotImplementedException();
        }
    }
}
