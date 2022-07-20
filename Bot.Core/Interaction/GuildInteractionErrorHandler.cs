using System;

namespace Bot.Core.Interaction
{
    public class GuildInteractionErrorHandler : BaseInteractionErrorHandler<ulong>, IGuildInteractionErrorHandler
    {
        public GuildInteractionErrorHandler(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {}

        protected override string GetFriendlyStringForKey(ulong key) => $"Guild: `{key}`";
    }
}
