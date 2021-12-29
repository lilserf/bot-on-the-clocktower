using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpClient : IBotClient
    {
        private readonly IEnvironment mEnvironment;

        public DSharpClient(IServiceProvider serviceProvider)
        {
            mEnvironment = serviceProvider.GetService<IEnvironment>();
        }

        public Task ConnectAsync()
        {
            var token = mEnvironment.GetEnvironmentVariable("DISCORD_TOKEN");

            if (string.IsNullOrWhiteSpace(token)) throw new InvalidDiscordTokenException();

            // TODO: not sure yet!
            return Task.CompletedTask;
        }
        public class InvalidDiscordTokenException : Exception { }
    }
}
