using System;
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
            throw new NotImplementedException();
        }
    }
}
