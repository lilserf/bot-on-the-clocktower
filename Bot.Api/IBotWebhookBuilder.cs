namespace Bot.Api
{
    public interface IBotWebhookBuilder
    {
        IBotWebhookBuilder WithContent(string content);

        IBotWebhookBuilder AddComponents(params IBotComponent[] components);
    }
}