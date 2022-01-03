using Bot.Api;
using Bot.Base;
using DSharpPlus;
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

        public IBotWebhookBuilder CreateWebhookBuilder() => new DSharpWebhookBuilder(new DiscordWebhookBuilder());

        // TODO: allow for selection of a button style
        public IComponent CreateButton(string customId, string label, bool disabled=false) 
            => new DSharpComponent(new DiscordButtonComponent(ButtonStyle.Primary, customId, label, disabled));
	}
}
