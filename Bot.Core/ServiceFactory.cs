﻿using Bot.Api;
using Bot.Api.Lookup;
using Bot.Base;
using Bot.Core.Callbacks;
using Bot.Core.Lookup;
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
            sp.AddService<ICallbackSchedulerFactory>(new CallbackSchedulerFactory(sp));
            sp.AddService<IActiveGameService>(new ActiveGameService());
            sp.AddService<IComponentService>(new ComponentService());
            sp.AddService<IShuffleService>(new ShuffleService());

            sp.AddService<IStringDownloader>(new StringDownloader(sp));
            sp.AddService<ICustomScriptCache>(new CustomScriptCache(sp));
            sp.AddService<IOfficialCharacterCache>(new OfficialCharacterCache(sp));
            sp.AddService<ICharacterStorage>(new CharacterStorage(sp));
            sp.AddService<ICharacterLookup>(new CharacterLookup(sp));
            return sp;
        }

        /// <summary>
        /// These services are ones that depend on a System being created and registered
        /// </summary>
        public static IServiceProvider RegisterBotServices(IServiceProvider? parentServices)
        {
            ServiceProvider sp = new(parentServices);
            sp.AddService<ITownCommandQueue>(new TownCommandQueue(sp));
            sp.AddService<ITownCleanup>(new TownCleanup(sp));
            sp.AddService<ITownResolver>(new TownResolver(sp));
            var gameplay = new BotGameplay(sp);
            sp.AddService<IVoteHandler>(gameplay);
            var voteTimer = new BotVoteTimer(sp);
            sp.AddService<IBotGameplayInteractionHandler>(new BotGameplayInteractionHandler(sp, gameplay, voteTimer));
            sp.AddService<IBotMessaging>(new BotMessaging(sp));
            sp.AddService<IBotSetup>(new BotSetup(sp));
            return sp;
        }
    }
}
