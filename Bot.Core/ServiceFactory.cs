﻿using Bot.Api;
using Bot.Base;
using System;

namespace Bot.Core
{
    public static class ServiceFactory
    {
        public static IServiceProvider RegisterServices(IServiceProvider? parentServices)
        {
            ServiceProvider sp = new(parentServices);
            sp.AddService<IBotGameService>(new BotGameService());
            return sp;
        }
    }
}