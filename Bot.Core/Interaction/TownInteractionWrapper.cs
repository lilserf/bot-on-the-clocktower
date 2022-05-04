using Bot.Api;
using System;

namespace Bot.Core.Interaction
{
    public class TownInteractionWrapper : BaseInteractionWrapper<TownKey, ITownInteractionQueue, ITownInteractionErrorHandler>, ITownInteractionWrapper
    {
        public TownInteractionWrapper(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}

        protected override TownKey KeyFromContext(IBotInteractionContext context) => context.GetTownKey();
    }
}
