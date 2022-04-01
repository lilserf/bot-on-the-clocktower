using Bot.Api;

namespace Bot.Core
{
    public class GuildInteractionErrorHandler : BaseInteractionErrorHandler<TownKey>, IGuildInteractionErrorHandler
    {
        protected override string GetFriendlyStringForKey(TownKey key) => InteractionWrapper.GetFriendlyStringForTownKey(key);
    }
}
