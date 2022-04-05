using System.Collections.Generic;

namespace Bot.Core.Lookup
{
    public interface IOfficialUrlProvider
    {
        IReadOnlyCollection<string> ScriptUrls { get; }
        IReadOnlyCollection<string> CharacterUrls { get; }
    }
}
