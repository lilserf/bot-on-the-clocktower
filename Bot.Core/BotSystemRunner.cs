using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSystemRunner
    {
        private readonly IBotSystem mSystem;
        private readonly IServiceProvider mServiceProvider;

        public BotSystemRunner(IServiceProvider serviceProvider, IBotSystem system)
        {
            mServiceProvider = serviceProvider;
            mSystem = system;
        }

        public Task RunAsync()
        {
            var _ = mSystem.CreateClient(mServiceProvider);

            // TODO!

            return Task.CompletedTask;
        }
    }
}
