using Bot.Api;
using System;
using System.Threading;
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

        public async Task RunAsync(CancellationToken cancelToken)
        {
            var client = mSystem.CreateClient(mServiceProvider);

            TaskCompletionSource tcs = new();

            using (cancelToken.Register(tcs.SetCanceled))
            {
                await Task.WhenAny(tcs.Task, client.ConnectAsync());
            }

            // Wait forever or until canceled
            await Task.Delay(-1, cancelToken);
        }
    }
}
