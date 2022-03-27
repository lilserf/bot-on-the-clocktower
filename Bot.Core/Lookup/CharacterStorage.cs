using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CharacterStorage : ICharacterStorage
    {
        private readonly IOfficialCharacterCache m_officialCache;

        public CharacterStorage(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_officialCache);
        }

        public async Task<GetCharactersResult> GetCharactersAsync(ulong guildId)
        {
            List<GetCharactersItem> items = new();
            var result = await m_officialCache.GetOfficialCharactersAsync();

            foreach (var i in result.Items)
                items.Add(new GetCharactersItem(i.Character, i.Scripts));

            return new GetCharactersResult(items);
        }
    }
}
