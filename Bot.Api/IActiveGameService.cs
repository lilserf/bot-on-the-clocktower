using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IActiveGameService
	{
		bool TryGetGame(IBotInteractionContext context, out IGame? game);
		bool RegisterGame(ITown town, IGame game);
	}
}
