using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IDatabaseFactory
	{
		// Probably unneeded with Services
		ITownLookup? TownLookup { get; }
		// Connect to the database and return a new service provider with all the services you registered
		public IServiceProvider Connect();

	}
}
