using System.Collections.Generic;
using System.Linq;

namespace Bot.Core.Lookup
{
    public class GetCustomScriptResult
    {
        public static readonly GetCustomScriptResult EmptyResult = new GetCustomScriptResult(Enumerable.Empty<ScriptWithCharacters>());

        public IReadOnlyCollection<ScriptWithCharacters> ScriptsWithCharacters { get; }
        public GetCustomScriptResult(IEnumerable<ScriptWithCharacters> scriptsWithCharacters)
        {
            ScriptsWithCharacters = scriptsWithCharacters.ToArray();
        }
    }
}
