using Bot.Api;
using Bot.Base;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class LookupMessageSender : ILookupMessageSender
    {
        public LookupMessageSender(ServiceProvider serviceProvider)
        {
        }

        public Task SendLookupMessageAsync(IChannel channel, LookupCharacterItem lookupItem)
        {
            throw new System.NotImplementedException();
        }
    }
}
