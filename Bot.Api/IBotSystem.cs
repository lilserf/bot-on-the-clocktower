using System;

namespace Bot.Api
{
    public interface IBotSystem
    {
        IBotClient CreateClient(IServiceProvider serviceProvider);

        IBotWebhookBuilder CreateWebhookBuilder();

        IInteractionResponseBuilder CreateInteractionResponseBuilder();

        enum ButtonType
		{
            Primary = 1,
            Secondary = 2,
            Success = 3,
            Danger = 4,
		}
        IBotComponent CreateButton(string customId, string label, ButtonType type = ButtonType.Primary, bool disabled = false);

    }
}
