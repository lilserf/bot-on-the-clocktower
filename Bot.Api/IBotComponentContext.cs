using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IBotComponentContext
	{
		public string CustomId { get; }

		Task DeferInteractionResponse();
		Task EditResponseAsync(IBotWebhookBuilder webhookBuilder);
		Task UpdateOriginalMessageAsync(IInteractionResponseBuilder builder);

	}
}
