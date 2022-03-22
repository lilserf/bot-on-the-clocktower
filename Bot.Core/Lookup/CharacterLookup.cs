using Bot.Api.Lookup;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CharacterLookup : ICharacterLookup
    {
        public CharacterLookup(IServiceProvider serviceProvider)
        {
        }

        public Task<LookupCharacterResult?> LookupCharacterAsync(ulong guildId, string charString)
        {
            LookupCharacterResult? ret = null;
            return Task.FromResult(ret);
        }
    }
}
