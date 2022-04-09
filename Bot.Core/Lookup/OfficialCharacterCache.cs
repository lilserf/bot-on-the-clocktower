using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class OfficialCharacterCache : IOfficialCharacterCache
    {
        private readonly IDateTime DateTime;

        private readonly IOfficialUrlProvider m_urlProvider;
        private readonly IOfficialScriptParser m_scriptParser;
        private readonly IStringDownloader m_downloader;

        private GetOfficialCharactersResult? m_lastResult;
        private DateTime m_lastResultTime = System.DateTime.MinValue;

        public OfficialCharacterCache(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out DateTime);

            serviceProvider.Inject(out m_urlProvider);
            serviceProvider.Inject(out m_scriptParser);
            serviceProvider.Inject(out m_downloader);
        }

        public async Task<GetOfficialCharactersResult> GetOfficialCharactersAsync()
        {
            var now = DateTime.Now;

            if (m_lastResult != null && m_lastResultTime > now - TimeSpan.FromDays(1))
                return m_lastResult;

            var scriptTasks = m_urlProvider.ScriptUrls.Select(m_downloader.DownloadStringAsync);
            var charTasks = m_urlProvider.CharacterUrls.Select(m_downloader.DownloadStringAsync);

            await Task.WhenAll(scriptTasks.Concat(charTasks).ToList());

            var validScripts = scriptTasks.Select(t => t.Result.Data).Where(d => d != null).Select(d => d!);
            var validChars = charTasks.Select(t => t.Result.Data).Where(d => d != null).Select(d => d!);

            m_lastResult = m_scriptParser.ParseOfficialData(validScripts, validChars);
            m_lastResultTime = now;
            return m_lastResult;
        }

        public void Invalidate()
        {
            m_lastResult = null;
        }
    }
}
