using Bot.Api;
using Bot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
	public static class ServiceFactory
	{
		public static IServiceProvider RegisterServices(IServiceProvider? parentServices)
		{
			ServiceProvider sp = new(parentServices);
			sp.AddService<IBotSystem>(new DSharpSystem());
			return sp;
		}
	}
}
