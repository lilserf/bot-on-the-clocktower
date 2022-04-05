using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotLookupService
    {
        Task LookupAsync(IBotInteractionContext ctx, string lookupString);
        Task AddScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl);
        Task RemoveScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl);
        Task ListScriptsAsync(IBotInteractionContext ctx);
    }
}
