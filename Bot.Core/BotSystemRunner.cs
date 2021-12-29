using Bot.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSystemRunner
    {
        private readonly IBotSystem m_system;
        private readonly IServiceProvider m_serviceProvider;

        public BotSystemRunner(IServiceProvider serviceProvider, IBotSystem system)
        {
            m_serviceProvider = serviceProvider;
            m_system = system;
        }

        public async Task RunAsync(CancellationToken cancelToken)
        {
            var client = m_system.CreateClient(m_serviceProvider);

            TaskCompletionSource tcs = new();

            using (cancelToken.Register(tcs.SetCanceled))
            {
                await Task.WhenAny(tcs.Task, client.ConnectAsync());
                await Task.WhenAny(tcs.Task, Task.Delay(-1, CancellationToken.None)); // avoiding TaskCanceledException
            }
        }
    }
}
