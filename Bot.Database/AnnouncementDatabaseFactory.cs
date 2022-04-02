using Bot.Api.Database;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    public interface IAnnouncementDatabaseFactory
    {
        IAnnouncementDatabase CreateAnnouncementDatabase(IMongoDatabase db);
    }
    public class AnnouncementDatabaseFactory : IAnnouncementDatabaseFactory
    {
        public IAnnouncementDatabase CreateAnnouncementDatabase(IMongoDatabase db)
        {
            return new AnnouncementDatabase(db);
        }
    }
}
