using System.Collections.Generic;

namespace Bot.Core.Lookup
{
    public class OfficialUrlProvider : IOfficialUrlProvider
    {
        public IReadOnlyCollection<string> ScriptUrls => s_editionUrls;

        public IReadOnlyCollection<string> CharacterUrls => s_characterUrls;


        private static readonly string[] s_editionUrls = new[]
        {
            "https://raw.githubusercontent.com/bra1n/townsquare/develop/src/editions.json",
        };

        private static readonly string[] s_characterUrls = new[]
        {
            "https://raw.githubusercontent.com/bra1n/townsquare/develop/src/roles.json",
            "https://raw.githubusercontent.com/bra1n/townsquare/develop/src/fabled.json",
        };
    }
}
