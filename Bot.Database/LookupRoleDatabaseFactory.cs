using Bot.Api.Database;
using MongoDB.Driver;

namespace Bot.Database
{
    public interface ILookupRoleDatabaseFactory
    {
        ILookupRoleDatabase CreateLookupRoleDatabase(IMongoDatabase db);
    }

    public class LookupRoleDatabaseFactory : ILookupRoleDatabaseFactory
    {
        public ILookupRoleDatabase CreateLookupRoleDatabase(IMongoDatabase db)
        {
            return new LookupRoleDatabase(db);
        }
    }
}
