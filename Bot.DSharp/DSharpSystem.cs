using Bot.Api;
using DSharpPlus;
using DSharpPlus.Entities;
using System;

namespace Bot.DSharp
{
    public class DSharpSystem : IBotSystem
    {
        public IBotClient CreateClient(IServiceProvider serviceProvider)
        {
            return new DSharpClient(serviceProvider);
        }

        public IBotWebhookBuilder CreateWebhookBuilder() => new DSharpWebhookBuilder(new DiscordWebhookBuilder());

        // TODO: allow for selection of a button style
        private static ButtonStyle TranslateButtonStyle(IBotSystem.ButtonType type)
		{
            return type switch
            {
                IBotSystem.ButtonType.Primary => ButtonStyle.Primary,
                IBotSystem.ButtonType.Secondary => ButtonStyle.Secondary,
                IBotSystem.ButtonType.Success => ButtonStyle.Success,
                IBotSystem.ButtonType.Danger => ButtonStyle.Danger,
                _ => throw new InvalidCastException("Can't translate this button type to a Discord button style!"),
            };
        }
        public IBotComponent CreateButton(string customId, string label, IBotSystem.ButtonType type, bool disabled=false) 
            => new DSharpComponent(new DiscordButtonComponent(TranslateButtonStyle(type), customId, label, disabled));

        public IInteractionResponseBuilder CreateInteractionResponseBuilder() => new DSharpInteractionResponseBuilder(new DiscordInteractionResponseBuilder());
	}
}
