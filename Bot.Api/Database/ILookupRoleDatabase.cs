using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface ILookupRoleDatabase
    {
        Task<IEnumerable<string>> GetScriptUrls(IGuild guild);

        Task AddScriptUrl(IGuild guild, string url);

        Task RemoveScriptUrl(IGuild guild, string url);
    }
}
