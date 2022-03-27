using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CustomScriptCache : ICustomScriptCache
    {
        public CustomScriptCache(IServiceProvider serviceProvider)
        {
        }

        public Task<GetCustomScriptResult> GetCustomScriptAsync(string url)
        {
            return Task.FromResult(new GetCustomScriptResult(Enumerable.Empty<ScriptWithCharacters>()));
        }
    }
}
