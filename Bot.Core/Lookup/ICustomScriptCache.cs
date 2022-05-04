using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface ICustomScriptCache
    {
        Task<GetCustomScriptResult> GetCustomScriptAsync(string url);
        void InvalidateCache(string url);
    }
}
