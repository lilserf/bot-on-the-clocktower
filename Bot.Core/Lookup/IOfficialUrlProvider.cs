using System.Collections.Generic;

namespace Bot.Core.Lookup
{
    public interface IOfficialUrlProvider
    {
        string RawSourceRoot { get; }
        IReadOnlyCollection<string> ScriptUrls { get; }
        IReadOnlyCollection<string> CharacterUrls { get; }
    }
}
