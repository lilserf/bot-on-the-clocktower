using System.Collections.Generic;
using System.Linq;

namespace Bot.Core.Lookup
{
    public class ScriptWithCharacters
    {
        public ScriptData Script { get; }
        public IReadOnlyCollection<CharacterData> Characters { get; }

        public ScriptWithCharacters(ScriptData script, IEnumerable<CharacterData> characters)
        {
            Script = script;
            Characters = characters.ToArray();
        }

        public override string ToString() => $"{Script} [{Characters.Aggregate(string.Empty, (s,c) => $"{s},{c}")}]";
    }
}
