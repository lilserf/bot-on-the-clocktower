using Bot.Api.Database;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    interface IGameActivityDatabaseFactory
    {
        IGameActivityDatabase CreateGameActivityDatabase(IMongoDatabase db);
    }

    public class GameActivityDatabaseFactory : IGameActivityDatabaseFactory
    {
        public IGameActivityDatabase CreateGameActivityDatabase(IMongoDatabase db)
        {
            return new GameActivityDatabase(db);
        }
    }
}
