using System;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class StringDownloader : IStringDownloader
    {
        public StringDownloader(IServiceProvider sp)
        {
        }

        public Task<DownloadResult> DownloadStringAsync(string url)
        {
            //https://stackoverflow.com/a/5566989/10606
            // though we need an async version somehow
            throw new System.NotImplementedException();
        }
    }
}
