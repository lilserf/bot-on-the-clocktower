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
            return new DiscordClientWrapper(new DiscordClient(config));
        }
    }
}
