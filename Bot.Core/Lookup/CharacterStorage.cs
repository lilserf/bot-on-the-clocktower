using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Api.Database;

namespace Bot.Core.Lookup
{
    public class CharacterStorage : ICharacterStorage
    {
        private readonly ILookupRoleDatabase m_lookupDb;
        private readonly IOfficialCharacterCache m_officialCache;
        private readonly ICustomScriptCache m_customCache;

        public CharacterStorage(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_lookupDb);
            serviceProvider.Inject(out m_officialCache);
            serviceProvider.Inject(out m_customCache);
        }

        public async Task<GetCharactersResult> GetCharactersAsync(ulong guildId)
        {
            List<GetCharactersItem> items = new();
            var officialTask = m_officialCache.GetOfficialCharactersAsync();

            var guildCustomScriptUrls = await m_lookupDb.GetScriptUrlsAsync(guildId);
            var customScriptTasks = guildCustomScriptUrls.Select(m_customCache.GetCustomScriptAsync).ToArray();

            await Task.WhenAll(officialTask, Task.WhenAll(customScriptTasks));

            foreach (var i in officialTask.Result.Items)
                items.Add(new GetCharactersItem(i.Character, i.Scripts));

            foreach (var t in customScriptTasks)
                foreach (var swc in t.Result.ScriptsWithCharacters)
                    foreach (var c in swc.Characters)
                        items.Add(new GetCharactersItem(c, new[] { swc.Script }));

            return new GetCharactersResult(items);
        }

        public async Task RefreshCharactersAsync(ulong guildId)
        {
            var guildCustomScriptUrls = await m_lookupDb.GetScriptUrlsAsync(guildId);

            m_officialCache.InvalidateCache();
            foreach (var url in guildCustomScriptUrls)
                m_customCache.InvalidateCache(url);
        }
    }
}
