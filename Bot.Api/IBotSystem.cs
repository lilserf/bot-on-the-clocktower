using System;

namespace Bot.Api
{
    public interface IBotSystem
    {
        IBotClient CreateClient(IServiceProvider serviceProvider);

        IBotWebhookBuilder CreateWebhookBuilder();

    }
}
