using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotLookupService
    {
        Task LookupAsync(string lookupString);
        Task AddScriptAsync(string scriptJsonUrl);
        Task RemoveScriptAsync(string scriptJsonUrl);
    }
}
