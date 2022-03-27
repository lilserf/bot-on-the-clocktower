using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface ICustomScriptCache
    {
        Task<GetCustomScriptResult> GetCustomScriptAsync(string url);
    }

    public class GetCustomScriptResult
    {
        public IReadOnlyCollection<ScriptWithCharacters> ScriptsWithCharacters { get; }
        public GetCustomScriptResult(IEnumerable<ScriptWithCharacters> scriptsWithCharacters)
        {
            ScriptsWithCharacters = scriptsWithCharacters.ToArray();
        }
    }
}
