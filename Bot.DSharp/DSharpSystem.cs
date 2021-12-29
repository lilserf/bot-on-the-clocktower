using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpSystem : IBotSystem
    {
        private readonly IEnvironment mEnvironment;

        public DSharpSystem(IServiceProvider serviceProvider)
        {
            mEnvironment = serviceProvider.GetService<IEnvironment>();
        }

        public Task InitializeAsync()
        {
            var token = mEnvironment.GetEnvironmentVariable("DISCORD_TOKEN");

            if (string.IsNullOrWhiteSpace(token)) throw new InvalidDiscordTokenException();

            // TODO: not sure yet!
            return Task.CompletedTask;
        }

        public class InvalidDiscordTokenException : Exception {}
    }
}
