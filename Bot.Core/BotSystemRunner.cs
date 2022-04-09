using Bot.Api;
using Bot.Base;
using Bot.Core.Lookup;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSystemRunner
    {
        private readonly IServiceProvider m_serviceProvider;
        private readonly IBotClient m_client;
        private readonly IFinalShutdownService m_finalShutdown;

        public BotSystemRunner(IServiceProvider parentServices, IBotSystem system)
        {
            parentServices.Inject(out m_finalShutdown);

            m_client = system.CreateClient(parentServices);

            var systemServices = new ServiceProvider(parentServices);
            systemServices.AddService(system);
            systemServices.AddService(m_client);

            m_serviceProvider = ServiceFactory.RegisterBotServices(systemServices);
            m_serviceProvider = LookupServiceFactory.RegisterBotLookupServices(m_serviceProvider);

            m_serviceProvider.GetService<IVersionProvider>().InitializeVersions();
        }

        public async Task RunAsync()
        {
            await m_client.ConnectAsync(m_serviceProvider);
            await m_finalShutdown.ReadyToShutdown;
            await m_client.DisconnectAsync();
        }
    }
}
