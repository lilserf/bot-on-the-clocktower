using System;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class UrlDownloader : IUrlDownloader
    {
        public UrlDownloader(IServiceProvider sp)
        {
        }

        public Task<DownloadResult> DownloadUrlAsync(string url)
        {
            throw new System.NotImplementedException();
        }
    }
}
