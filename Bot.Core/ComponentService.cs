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
		Dictionary<string, Func<IBotInteractionContext, Task>> m_callbacks;
		public ComponentService()
		{
			m_callbacks = new();
		}

		public async Task<bool> CallAsync(IBotInteractionContext context)
		{
			if (context.ComponentCustomId == null) throw new InvalidOperationException("Somehow tried to process an interaction without a custom ID!");
			Func<IBotInteractionContext, Task> cb;
			if(m_callbacks.TryGetValue(context.ComponentCustomId, out cb!))
			{
				await cb(context);
				return true;
			}
			return false;
		}

		public void RegisterComponent(IBotComponent? component, Func<IBotInteractionContext, Task> callback)
		{
			if (m_callbacks.ContainsKey(component.CustomId)) throw new InvalidOperationException("Component is already registered!");

			m_callbacks[component.CustomId] = callback;
		}
	}
}
