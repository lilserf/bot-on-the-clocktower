using DSharpPlus;

namespace Bot.DSharp
{
    public interface IDiscordClientFactory
    {
        IDiscordClient CreateClient(DiscordConfiguration config);
    }

    public class DiscordClientFactory : IDiscordClientFactory
    {
        public IDiscordClient CreateClient(DiscordConfiguration config)
        {
            var client = new DiscordShardedClient(config);
            return new DiscordClientWrapper(client);
        }
    }
}
