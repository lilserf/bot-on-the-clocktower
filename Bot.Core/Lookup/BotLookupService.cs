using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class BotLookupService : IBotLookupService
    {
        public BotLookupService(IServiceProvider serviceProvider)
        {
        }

        public Task LookupAsync(string lookupString)
        {
            throw new System.NotImplementedException();
        }
        public Task AddScriptAsync(string scriptJsonUrl)
        {
            throw new System.NotImplementedException();
        }

        public Task RemoveScriptAsync(string scriptJsonUrl)
        {
            throw new System.NotImplementedException();
        }
    }
}
