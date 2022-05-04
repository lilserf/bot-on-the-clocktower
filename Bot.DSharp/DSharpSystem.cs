using Bot.Api;
using Bot.DSharp;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public IBotComponent CreateButton(string customId, string label, IBotSystem.ButtonType type, bool disabled=false, string? emoji=null) 
            => new DSharpComponent(new DiscordButtonComponent(TranslateButtonStyle(type), customId, label, disabled, emoji != null ? new DiscordComponentEmoji(emoji):null));

        public IBotComponent CreateSelectMenu(string customId, string placeholder, IEnumerable<IBotSystem.SelectMenuOption> options, bool disabled = false, int minOptions = 1, int maxOptions = 1)
        {
            var dOptions = options.Select(x => new DiscordSelectComponentOption(x.Label, x.Value, x.Description, x.IsDefault, x.Emoji != null ? new DiscordComponentEmoji(x.Emoji) : null));
            var menu = new DSharpComponent(new DiscordSelectComponent(customId, placeholder, dOptions, disabled, minOptions, maxOptions));
            return menu;
        }

        public IInteractionResponseBuilder CreateInteractionResponseBuilder() => new DSharpInteractionResponseBuilder(new DiscordInteractionResponseBuilder());


        public IBotComponent CreateTextInput(string customId, string label, string? placeholder=null, string? value=null, bool required=true)
        {
            var text1 = new TextInputComponent(label, customId, placeholder, value, required);
            return new DSharpComponent(text1);
        }

        public IEmbedBuilder CreateEmbedBuilder() => new DSharpEmbedBuilder(new DiscordEmbedBuilder());
        public IColorBuilder ColorBuilder { get; } = new DSharpColorBuilder();

        public IMessageBuilder CreateMessageBuilder() => new DSharpMessageBuilder(new DiscordMessageBuilder());
    }
}
