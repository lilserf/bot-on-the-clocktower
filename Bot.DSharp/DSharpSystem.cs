using Bot.Api;
using Bot.Base;
using DSharpPlus.Entities;
using System;

namespace Bot.DSharp
{
    public class DSharpSystem : IBotSystem
    {
        public IBotClient CreateClient(IServiceProvider serviceProvider)
        {
            ServiceProvider sp = new(serviceProvider);
            sp.AddService<IBotSystem>(this);
            return new DSharpClient(sp);
        }

        public IBotInteractionResponseBuilder CreateInteractionResponseBuilder() => new DSharpInteractionResponseBuilder(new DiscordInteractionResponseBuilder());
    }
}
