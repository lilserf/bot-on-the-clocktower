using Bot.Base;
using Bot.DSharp.DiscordWrappers;
using System;

namespace Bot.DSharp
{
    public static class ServiceFactory
	{
		public static IServiceProvider RegisterServices(IServiceProvider? parentServices)
		{
			ServiceProvider sp = new(parentServices);
			sp.AddService<IDiscordClientFactory>(new DiscordClientFactory());
			return sp;
		}
	}
}
