using Bot.Api;
using Bot.Base;
using System;

namespace Bot.Core.Lookup
{
    public static class LookupServiceFactory
    {
        public static IServiceProvider RegisterCoreLookupServices(IServiceProvider? parentServices)
        {
            ServiceProvider sp = new(parentServices);

            sp.AddService<IOfficialUrlProvider>(new OfficialUrlProvider());
            sp.AddService<IOfficialScriptParser>(new OfficialScriptParser());
            sp.AddService<ICustomScriptParser>(new CustomScriptParser());
            sp.AddService<IStringDownloader>(new StringDownloader());
            sp.AddService<ICustomScriptCache>(new CustomScriptCache(sp));
            sp.AddService<IOfficialCharacterCache>(new OfficialCharacterCache(sp));
            sp.AddService<ICharacterStorage>(new CharacterStorage(sp));
            sp.AddService<ICharacterLookup>(new CharacterLookup(sp));
            sp.AddService<ILookupMessageSender>(new LookupMessageSender(sp));

            return sp;
        }

        public static IServiceProvider RegisterBotLookupServices(IServiceProvider serviceProvider)
        {
            ServiceProvider sp = new(serviceProvider);

            sp.AddService<IBotLookupService>(new BotLookupService(sp));

            return sp;
        }
    }
}
