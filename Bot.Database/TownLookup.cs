using Bot.Api;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
	class TownLookup : ITownLookup
	{
		IMongoDatabase? m_guildInfo;

		public TownLookup()
		{
			
		}

		public void Connect(IMongoClient client)
		{
			m_guildInfo = client.GetDatabase("GuildInfo");

			if (m_guildInfo == null)
			{
				throw new MissingGuildInfoDatabaseException();
			}
		}

		public class MissingGuildInfoDatabaseException : Exception { }
	}
}
