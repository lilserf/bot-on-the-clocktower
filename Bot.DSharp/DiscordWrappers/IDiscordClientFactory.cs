using DSharpPlus;

namespace Bot.DSharp.DiscordWrappers
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
