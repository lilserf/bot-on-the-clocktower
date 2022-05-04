using Bot.Api;
using System;

namespace Bot.Core.Interaction
{
    public class GuildInteractionWrapper : BaseInteractionWrapper<ulong, IGuildInteractionQueue, IGuildInteractionErrorHandler>, IGuildInteractionWrapper
    {
        public GuildInteractionWrapper(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}

        protected override ulong KeyFromContext(IBotInteractionContext context) => context.Guild.Id;
    }
}
