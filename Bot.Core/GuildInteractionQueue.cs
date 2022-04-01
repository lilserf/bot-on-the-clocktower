using Bot.Api;
using System;

namespace Bot.Core
{
    public class GuildInteractionQueue : BaseInteractionQueue<ulong>, IGuildInteractionQueue
    {
        public GuildInteractionQueue(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}

        protected override ulong KeyFromContext(IBotInteractionContext context) => context.Guild.Id;
    }
}
