using System;

namespace Bot.Api
{
    public interface IBotSystem
    {
        IBotClient CreateClient(IServiceProvider serviceProvider);

        IBotWebhookBuilder CreateWebhookBuilder();

        IComponent CreateButton(string customId, string label, bool disabled = false);

    }
}
