using Bot.Api;
using DSharpPlus.SlashCommands;
using System;

namespace Bot.DSharp
{
    internal interface ISlashCommandModuleWithClientContext
    {
        void SetClientContext(IBotClient client, IServiceProvider serviceProvider);
    }

    internal class SlashCommandModuleWithClientContext : SlashCommandModule
    {
        protected IBotClient Client
        {
            get
            {
                if (m_client == null) throw new InvalidOperationException("Must set up client context before accepting commands");
                return m_client!;
            }
        }

        protected IServiceProvider Services
        {
            get
            {
                if (m_services == null) throw new InvalidOperationException("Must set up client context before accepting commands");
                return m_services!;
            }
        }

        private IBotClient? m_client = null;
        private IServiceProvider? m_services = null;

        public void SetClientContext(IBotClient client, IServiceProvider serviceProvider)
        {
            m_client = client;
            m_services = serviceProvider;
        }
    }
}
