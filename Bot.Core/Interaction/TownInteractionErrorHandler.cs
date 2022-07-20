using Bot.Api;
using System;

namespace Bot.Core.Interaction
{
    public class TownInteractionErrorHandler : BaseInteractionErrorHandler<TownKey>, ITownInteractionErrorHandler
    {
        public TownInteractionErrorHandler(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {}

        protected override string GetFriendlyStringForKey(TownKey townKey) => $"Guild: `{townKey.GuildId}`\nChannel: `{townKey.ControlChannelId}`";
    }
}
