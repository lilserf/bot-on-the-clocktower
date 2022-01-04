using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IInteractionResponseBuilder
	{
		IInteractionResponseBuilder WithContent(string content);

		IInteractionResponseBuilder AddComponents(params IBotComponent[] components);
	}
}
