using System.Collections.Generic;

namespace Bot.Core.Lookup
{
    public interface IOfficialScriptParser
    {
        GetOfficialCharactersResult ParseOfficialData(IEnumerable<string> scriptJsons, IEnumerable<string> characterJsons);
    }
}
