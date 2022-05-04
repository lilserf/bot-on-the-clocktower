using System.Collections.Generic;

namespace Bot.Core.Lookup
{
    public class OfficialUrlProvider : IOfficialUrlProvider
    {
        public IReadOnlyCollection<string> ScriptUrls => s_editionUrls;

        public IReadOnlyCollection<string> CharacterUrls => s_characterUrls;

        public string RawSourceRoot => s_rawSourceRoot;

        private const string s_rawSourceRoot = "https://raw.githubusercontent.com/bra1n/townsquare/develop/src/";

        private static readonly string[] s_editionUrls = new[]
        {
            $"{s_rawSourceRoot}editions.json",
        };

        private static readonly string[] s_characterUrls = new[]
        {
            $"{s_rawSourceRoot}roles.json",
            $"{s_rawSourceRoot}fabled.json",
        };
    }
}
