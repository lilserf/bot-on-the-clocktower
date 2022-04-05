using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotClient
    {
        Task ConnectAsync(IServiceProvider serviceProvider);
        Task DisconnectAsync();

        Task<IGuild?> GetGuildAsync(ulong guildId);

        event EventHandler<EventArgs> Connected;
        event EventHandler<MessageCreatedEventArgs> MessageCreated;
    }

    public class MessageCreatedEventArgs : EventArgs
    {
        public IChannel Channel { get; }
        public string Message { get; }
        public MessageCreatedEventArgs(IChannel channel, string message)
        {
            Channel = channel;
            Message = message;
        }
    }
}
