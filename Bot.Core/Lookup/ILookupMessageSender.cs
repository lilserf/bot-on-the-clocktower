using Bot.Api;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface ILookupMessageSender
    {
        Task SendLookupMessageAsync(IChannel channel, LookupCharacterItem lookupItem);
    }
}
