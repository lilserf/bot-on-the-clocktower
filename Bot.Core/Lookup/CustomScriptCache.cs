using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CustomScriptCache : ICustomScriptCache
    {
        private readonly IDateTime DateTime;

        private readonly IStringDownloader m_stringDownloader;
        private readonly ICustomScriptParser m_scriptParser;

        private readonly Dictionary<string, (GetCustomScriptResult, DateTime)> m_urlToScriptAndTime = new();

        public CustomScriptCache(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out DateTime);

            serviceProvider.Inject(out m_stringDownloader);
            serviceProvider.Inject(out m_scriptParser);
        }

        public async Task<GetCustomScriptResult> GetCustomScriptAsync(string url)
        {
            var now = DateTime.Now;

            if (m_urlToScriptAndTime.TryGetValue(url, out var scriptAndTime) && scriptAndTime.Item2 > now - TimeSpan.FromDays(1))
                return scriptAndTime.Item1;

            string? json = (await m_stringDownloader.DownloadStringAsync(url)).Data;
            if (json == null)
                 return GetCustomScriptResult.EmptyResult;

            var script = m_scriptParser.ParseScript(json);
            m_urlToScriptAndTime[url] = (script, now);
            return script;
        }
    }
}
