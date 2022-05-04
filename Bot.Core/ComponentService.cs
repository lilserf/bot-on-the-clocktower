using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class ComponentService : IComponentService
	{
		private readonly Dictionary<string, Func<IBotInteractionContext, Task>> m_callbacks = new();

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

		public void RegisterComponent(IBotComponent component, Func<IBotInteractionContext, Task> callback)
		{
			if (m_callbacks.ContainsKey(component.CustomId)) throw new InvalidOperationException("Component is already registered!");

			m_callbacks.Add(component.CustomId, callback);
		}
	}
}
