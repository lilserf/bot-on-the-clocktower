using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class StringDownloader : IStringDownloader
    {
        public async Task<DownloadResult> DownloadStringAsync(string url)
        {
            string? data = null;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    data = await httpClient.GetStringAsync(url);
                }
                catch (Exception)
                {}
            }
            return new DownloadResult(data);
        }
    }
}
