using Bot.Api;

namespace Bot.Core.Lookup
{
    public interface ILookupEmbedBuilder
    {
        IEmbed BuildLookupEmbed(LookupCharacterItem lookupItem);
    }
}
