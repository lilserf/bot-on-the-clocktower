using System;
using System.Collections.Generic;

namespace Bot.Api
{
    public interface IBotSystem
    {
        IBotClient CreateClient(IServiceProvider serviceProvider);

        IBotWebhookBuilder CreateWebhookBuilder();

        IInteractionResponseBuilder CreateInteractionResponseBuilder();

        IEmbedBuilder CreateEmbedBuilder();

        IColorBuilder ColorBuilder { get; }

        enum ButtonType
		{
            Primary = 1,
            Secondary = 2,
            Success = 3,
            Danger = 4,
		}
        IBotComponent CreateButton(string customId, string label, ButtonType type = ButtonType.Primary, bool disabled = false, string? emoji = null);

        public struct SelectMenuOption
        {
            public string Label { get; }
            public string Value { get; }
            public string? Description { get; }
            public bool IsDefault { get; }
            public string? Emoji { get; }

            public SelectMenuOption(string label, string value, string? description = null, bool isDefault = false, string? emoji=null)
            {
                Label = label;
                Value = value;
                Description = description;
                IsDefault = isDefault;
                Emoji = emoji;
            }
        }
        
        IBotComponent CreateSelectMenu(string customId, string placeholder, IEnumerable<SelectMenuOption> options, bool disabled = false, int minOptions = 1, int maxOptions = 1);

        IBotComponent CreateTextInput(string customId, string label, string? placeholder = null, string? value = null, bool required = true);
    }
}
