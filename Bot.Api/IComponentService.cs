using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IComponentService
	{
		public void RegisterComponent(IBotComponent component, Func<IBotComponentContext, IServiceProvider, Task> callback);

		public Task<bool> CallAsync(IBotComponentContext context, IServiceProvider services);
	}
}
