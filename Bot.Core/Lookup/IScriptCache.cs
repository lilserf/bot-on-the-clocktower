using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface IScriptCache
    {
        public Task<GetScriptsResult> GetOfficialScriptsAsync();
    }

    public class GetScriptsResult
    {
        public IReadOnlyCollection<ScriptWithCharacters> Scripts { get; }

        public GetScriptsResult(IEnumerable<ScriptWithCharacters> scripts)
        {
            Scripts = scripts.ToArray();
        }
    }
}
