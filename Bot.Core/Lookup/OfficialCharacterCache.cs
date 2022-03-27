using System;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class OfficialCharacterCache : IOfficialCharacterCache
    {
        public OfficialCharacterCache(IServiceProvider serviceProvider)
        {
        }

        public Task<GetOfficialCharactersResult> GetOfficialCharactersAsync()
        {
            throw new NotImplementedException();
        }
    }
}
