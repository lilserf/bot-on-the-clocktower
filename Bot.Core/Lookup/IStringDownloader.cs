using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface IStringDownloader
    {
        Task<DownloadResult> DownloadStringAsync(string url);
    }

    public class DownloadResult
    {
        public string? Data { get; }

        public DownloadResult(string? data)
        {
            Data = data;
        }
    }
}
