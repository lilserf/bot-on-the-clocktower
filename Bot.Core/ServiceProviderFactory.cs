using Bot.Api;
using System;

namespace Bot.Core
{
    public class ServiceProviderFactory
    {
        public IServiceProvider CreateServiceProvider()
        {
            ServiceProvider sp = new();
            sp.AddService<IEnvironment>(new BotEnvironment());
            return sp;
        }
    }
}
