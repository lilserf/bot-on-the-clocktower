using Bot.Api;
using System;

namespace Bot.Core.Interaction
{
    public class TownInteractionQueue : BaseInteractionQueue<TownKey>, ITownInteractionQueue
    {
        public TownInteractionQueue(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}

        protected override TownKey KeyFromContext(IBotInteractionContext context) => context.GetTownKey();
        protected override string GetFriendlyStringForKey(TownKey townKey) => $"Guild: `{townKey.GuildId}`\nChannel: `{townKey.ControlChannelId}`";
    }
}
