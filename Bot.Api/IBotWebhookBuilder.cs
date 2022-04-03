using System.Collections.Generic;

namespace Bot.Api
{
    public interface IBotWebhookBuilder
    {
        IBotWebhookBuilder WithContent(string content);

        IBotWebhookBuilder AddComponents(params IBotComponent[] components);

        IBotWebhookBuilder AddEmbeds(IEnumerable<IEmbed> embeds);
    }

    public static class IBotWebhookBuilderExtensions
    {
        public static IBotWebhookBuilder AddEmbed(IBotWebhookBuilder @this, IEmbed embed)
        {
            return @this.AddEmbeds(new[] { embed });
        }
    }
}