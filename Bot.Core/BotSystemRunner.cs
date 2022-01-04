using Bot.Api;
using Bot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSystemRunner
    {
        private readonly IServiceProvider m_serviceProvider;
        private readonly IBotClient m_client;

        public BotSystemRunner(IServiceProvider parentServices, IBotSystem system)
        {
            m_client = system.CreateClient(parentServices);

            var systemServices = new ServiceProvider(parentServices);
            systemServices.AddService(system);
            systemServices.AddService(m_client);

            m_serviceProvider = ServiceFactory.RegisterBotServices(systemServices);
        }

        public async Task RunAsync(CancellationToken cancelToken)
        {
            TaskCompletionSource tcs = new();

            using (cancelToken.Register(tcs.SetCanceled))
            {
                await Task.WhenAny(tcs.Task, m_client.ConnectAsync(m_serviceProvider));
                await Task.WhenAny(tcs.Task, Task.Delay(-1, CancellationToken.None)); // avoiding TaskCanceledException
            }
        }
    }
}
