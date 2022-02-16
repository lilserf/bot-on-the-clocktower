using Bot.Api.Database;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    public interface ILookupRoleDatabaseFactory
    {
        ILookupRoleDatabase CreateLookupRoleDatabase(IMongoDatabase db);
    }
    class LookupRoleDatabaseFactory : ILookupRoleDatabaseFactory
    {
        public ILookupRoleDatabase CreateLookupRoleDatabase(IMongoDatabase db)
        {
            return new LookupRoleDatabase(db);
        }
    }
}
