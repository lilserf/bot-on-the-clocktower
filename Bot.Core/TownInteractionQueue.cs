using Bot.Api;
using System;

namespace Bot.Core
{
    public class TownInteractionQueue : BaseInteractionQueue<TownKey>, ITownInteractionQueue
    {
        public TownInteractionQueue(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}

        protected override TownKey KeyFromContext(IBotInteractionContext context) => context.GetTownKey();
    }
}
