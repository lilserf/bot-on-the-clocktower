using Bot.Api;
using Bot.Base;
using System;

namespace Bot.Core
{
    public static class ServiceProviderFactory
    {
        public static IServiceProvider CreateServiceProvider()
        {
            ServiceProvider sp = new();
            sp.AddService<IEnvironment>(new BotEnvironment());

            sp.AddService<IBotGameService>(new BotGameService());
            return sp;
        }
    }
}
