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
        private ButtonStyle TranslateButtonStyle(IBotSystem.ButtonType type)
		{
            switch(type)
			{
                case IBotSystem.ButtonType.Primary: return ButtonStyle.Primary;
                case IBotSystem.ButtonType.Secondary: return ButtonStyle.Secondary;
                case IBotSystem.ButtonType.Success: return ButtonStyle.Success;
                case IBotSystem.ButtonType.Danger: return ButtonStyle.Danger;
                default: throw new InvalidCastException("Can't translate this button type to a Discord button style!");
			}
		}
        public IBotComponent CreateButton(string customId, string label, IBotSystem.ButtonType type, bool disabled=false) 
            => new DSharpComponent(new DiscordButtonComponent(TranslateButtonStyle(type), customId, label, disabled));

        public IInteractionResponseBuilder CreateInteractionResponseBuilder() => new DSharpInteractionResponseBuilder(new DiscordInteractionResponseBuilder());
	}
}
