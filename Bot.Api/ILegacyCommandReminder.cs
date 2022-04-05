using System.Threading.Tasks;

namespace Bot.Api
{
    public interface ILegacyCommandReminder
    {
        Task UserMessageCreated(string message, IChannel channel);
    }
}
