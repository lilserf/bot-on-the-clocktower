using Bot.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSystemRunner
    {
        private readonly IServiceProvider m_serviceProvider;

        public BotSystemRunner(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
        }

        public async Task RunAsync(CancellationToken cancelToken)
        {
            // THIS FEELS HACKY
            // We need to have the Gameplay system (and maybe others later) initialize their components (Buttons etc) once
            // most of the other services exist (IBotSystem in particular)
            var gameplay = m_serviceProvider.GetService<IBotGameplay>();
            gameplay.CreateComponents(m_serviceProvider);

            var system = m_serviceProvider.GetService<IBotSystem>();
            var client = system.CreateClient(m_serviceProvider);

            TaskCompletionSource tcs = new();

            using (cancelToken.Register(tcs.SetCanceled))
            {
                await Task.WhenAny(tcs.Task, client.ConnectAsync());
                await Task.WhenAny(tcs.Task, Task.Delay(-1, CancellationToken.None)); // avoiding TaskCanceledException
            }
        }
    }
}
