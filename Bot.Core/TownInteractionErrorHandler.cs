using Bot.Api;

namespace Bot.Core
{
    public class TownInteractionErrorHandler : BaseInteractionErrorHandler<TownKey>, ITownInteractionErrorHandler
    {
        protected override string GetFriendlyStringForKey(TownKey townKey) => InteractionWrapper.GetFriendlyStringForTownKey(townKey);
    }
}
