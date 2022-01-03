namespace Bot.Api
{
    public interface IBotWebhookBuilder
    {
        IBotWebhookBuilder WithContent(string content);

        public IBotWebhookBuilder AddComponents(params IComponent[] components);
    }
}