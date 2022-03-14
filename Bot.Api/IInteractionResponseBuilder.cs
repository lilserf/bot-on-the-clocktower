using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IInteractionResponseBuilder
	{
		IInteractionResponseBuilder WithTitle(string title);
		IInteractionResponseBuilder WithCustomId(string customId);
		IInteractionResponseBuilder WithContent(string content);

		IInteractionResponseBuilder AddComponents(params IBotComponent[] components);
	}
}
