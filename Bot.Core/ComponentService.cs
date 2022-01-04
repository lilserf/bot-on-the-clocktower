using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
	class ComponentService : IComponentService
	{
		Dictionary<string, Func<IBotComponentContext, IServiceProvider, Task>> m_callbacks;
		public ComponentService()
		{
			m_callbacks = new();
		}

		public async Task<bool> CallAsync(IBotComponentContext context, IServiceProvider services)
		{
			Func<IBotComponentContext, IServiceProvider, Task> cb;
			if(m_callbacks.TryGetValue(context.CustomId, out cb!))
			{
				await cb(context, services);
				return true;
			}
			return false;
		}

		public void RegisterComponent(IBotComponent? component, Func<IBotComponentContext, IServiceProvider, Task> callback)
		{
			if (m_callbacks.ContainsKey(component.CustomId)) throw new InvalidOperationException("Component is already registered!");

			m_callbacks[component.CustomId] = callback;
		}
	}
}
