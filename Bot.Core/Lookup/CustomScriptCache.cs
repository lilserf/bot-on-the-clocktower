using System;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CustomScriptCache : ICustomScriptCache
    {
        private readonly IStringDownloader m_stringDownloader;
        private readonly ICustomScriptParser m_scriptParser;

        public CustomScriptCache(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_stringDownloader);
            serviceProvider.Inject(out m_scriptParser);
        }

        public async Task<GetCustomScriptResult> GetCustomScriptAsync(string url)
        {
            string? json = (await m_stringDownloader.DownloadStringAsync(url)).Data;
            if (json == null)
                return GetCustomScriptResult.EmptyResult;

            return m_scriptParser.ParseScript(json);
        }
    }
}
