using Bot.Api;

namespace Bot.Core.Interaction
{
    public class TownInteractionErrorHandler : BaseInteractionErrorHandler<TownKey>, ITownInteractionErrorHandler
    {
        protected override string GetFriendlyStringForKey(TownKey townKey) => $"Guild: `{townKey.GuildId}`\nChannel: `{townKey.ControlChannelId}`";
    }
}
