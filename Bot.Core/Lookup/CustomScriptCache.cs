using Bot.Api;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CustomScriptCache : ICustomScriptCache
    {
        private readonly IDateTime DateTime;

        private readonly IStringDownloader m_stringDownloader;
        private readonly ICustomScriptParser m_scriptParser;

        private readonly ConcurrentDictionary<string, (GetCustomScriptResult, DateTime)> m_urlToScriptAndTime = new();

        public CustomScriptCache(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out DateTime);

            serviceProvider.Inject(out m_stringDownloader);
            serviceProvider.Inject(out m_scriptParser);
        }

        public async Task<GetCustomScriptResult> GetCustomScriptAsync(string url)
        {
            var now = DateTime.Now;

            bool existsInDict = false;
            if (m_urlToScriptAndTime.TryGetValue(url, out var scriptAndTime))
                if (scriptAndTime.Item2 > now - TimeSpan.FromDays(1))
                    return scriptAndTime.Item1;
                else
                    existsInDict = true;

            string? json = (await m_stringDownloader.DownloadStringAsync(url)).Data;
            if (json == null)
                 return GetCustomScriptResult.EmptyResult;

            var script = m_scriptParser.ParseScript(json);
            if (!existsInDict || m_urlToScriptAndTime.TryRemove(url, out _))
                m_urlToScriptAndTime.TryAdd(url, (script, now));
            return script;
        }

        public void InvalidateCache(string url)
        {
            m_urlToScriptAndTime.TryRemove(url, out _);
        }
    }
}
