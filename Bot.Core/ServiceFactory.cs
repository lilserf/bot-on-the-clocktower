using Bot.Api;
using Bot.Base;
using Bot.Core.Callbacks;
using Bot.Core.Interaction;
using Serilog;
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

            var shutdown = new ShutdownService(sp, applicationCancelToken);
            sp.AddService<IFinalShutdownService>(shutdown);
            sp.AddService<IShutdownPreventionService>(shutdown);
            sp.AddService<IProcessLoggerFactory>(new ProcessLoggerFactory(sp.GetService<ILogger>()));
            sp.AddService<ITownInteractionErrorHandler>(new TownInteractionErrorHandler(sp));
            sp.AddService<IGuildInteractionErrorHandler>(new GuildInteractionErrorHandler(sp));

            sp.AddService<ICallbackSchedulerFactory>(new CallbackSchedulerFactory(sp));
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
            sp.AddService<IGuildInteractionQueue>(new GuildInteractionQueue(sp));
            sp.AddService<ITownInteractionQueue>(new TownInteractionQueue(sp));
            sp.AddService<IGuildInteractionWrapper>(new GuildInteractionWrapper(sp));
            sp.AddService<ITownInteractionWrapper>(new TownInteractionWrapper(sp));
            sp.AddService<ITownMaintenance>(new TownMaintenance(sp));
            sp.AddService<ITownCleanup>(new TownCleanup(sp));
            sp.AddService<ITownResolver>(new TownResolver(sp));
            var gameplay = new BotGameplay(sp);
            sp.AddService<IVoteHandler>(gameplay);
            var voteTimer = new BotVoteTimer(sp);
            sp.AddService<IBotGameplayInteractionHandler>(new BotGameplayInteractionHandler(sp, gameplay, voteTimer));
            sp.AddService<IBotMessaging>(new BotMessaging(sp));
            sp.AddService<IBotSetup>(new BotSetup(sp));
            sp.AddService<IVersionProvider>(new VersionProvider(sp));
            sp.AddService<IAnnouncer>(new Announcer(sp));
            sp.AddService<ILegacyCommandReminder>(new LegacyCommandReminder(sp));
            sp.AddService<IGhostTownCleanup>(new GhostTownCleanup(sp));
            return sp;
        }
    }
}
