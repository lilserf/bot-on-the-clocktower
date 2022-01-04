﻿using Bot.Api;
using Bot.Base;
using System;

namespace Bot.Core
{
    public static class ServiceFactory
    {
        /// <summary>
        /// These services are ones with few dependencies on anything
        /// </summary>
        public static IServiceProvider RegisterCoreServices(IServiceProvider? parentServices)
        {
            ServiceProvider sp = new(parentServices);
            sp.AddService<IActiveGameService>(new ActiveGameService());
            sp.AddService<IComponentService>(new ComponentService());
            return sp;
        }

        /// <summary>
        /// These services are ones that depend on a System being created and registered
        /// </summary>
        public static IServiceProvider RegisterBotServices(IServiceProvider? parentServices)
        {
            ServiceProvider sp = new(parentServices);
            sp.AddService<IBotGameplay>(new BotGameplay(sp));
            return sp;
        }
    }
}
