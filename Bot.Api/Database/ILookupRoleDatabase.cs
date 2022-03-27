using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface ILookupRoleDatabase
    {
        Task<IEnumerable<string>> GetScriptUrlsAsync(ulong guildId);

        Task AddScriptUrlAsync(ulong guildId, string url);

        Task RemoveScriptUrlAsync(ulong guildId, string url);
    }
}
