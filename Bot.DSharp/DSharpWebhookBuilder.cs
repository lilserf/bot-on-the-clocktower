using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Linq;

namespace Bot.DSharp
{
    public class DSharpWebhookBuilder : DiscordWrapper<DiscordWebhookBuilder>, IBotWebhookBuilder
    {
        public DSharpWebhookBuilder(DiscordWebhookBuilder wrapped)
            : base(wrapped)
        {
        }

        public IBotWebhookBuilder WithContent(string content)
        {
            var w2 = Wrapped.WithContent(content);
            if (w2 != Wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
            return this;
        }

        public IBotWebhookBuilder AddComponents(params IComponent[] components)
        {
            if (!components.All(x => x is DSharpComponent)) throw new InvalidOperationException("Unexpected type of IComponent!");
            var realComponents = components.Cast<DSharpComponent>().Select(x => x.Wrapped);
            var w2 = Wrapped.AddComponents(realComponents);
            if (w2 != Wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
            return this;
        }
    }
}
