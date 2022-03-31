using Bot.Api;
using Bot.Api.Lookup;
using Bot.Base;
using Bot.Core.Callbacks;
using System;
using System.Threading;

namespace Bot.Core
{
    public static class ServiceFactory
    {
        /// <summary>
        /// These services are ones with few dependencies on anything
        /// </summary>
        public static IServiceProvider RegisterCoreServices(IServiceProvider? parentServices, CancellationToken applicationCancelToken)
        {
            ServiceProvider sp = new(parentServices);

            var shutdown = new ShutdownService(applicationCancelToken);
            sp.AddService<IFinalShutdownService>(shutdown);
            sp.AddService<IShutdownPreventionService>(shutdown);

            sp.AddService<ICallbackSchedulerFactory>(new CallbackSchedulerFactory(sp));
            sp.AddService<IActiveGameService>(new ActiveGameService());
            sp.AddService<IComponentService>(new ComponentService());
            sp.AddService<IShuffleService>(new ShuffleService());
            return sp;
        }

        /// <summary>
        /// These services are ones that depend on a System being created and registered
        /// </summary>
        public static IServiceProvider RegisterBotServices(IServiceProvider? parentServices)
        {
            ServiceProvider sp = new(parentServices);
            sp.AddService<ITownInteractionQueue>(new TownInteractionQueue(sp));
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
