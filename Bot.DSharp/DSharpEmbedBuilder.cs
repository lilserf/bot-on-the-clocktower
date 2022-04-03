using System;
using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    internal class DSharpEmbedBuilder : DiscordWrapper<DiscordEmbedBuilder>, IEmbedBuilder
    {
        public DSharpEmbedBuilder(DiscordEmbedBuilder wrapped)
            : base(wrapped)
        {}

        public IEmbedBuilder AddField(string name, string value, bool inline = false)
        {
            var result = Wrapped.AddField(name, value, inline);
            if (result != Wrapped) throw new ApplicationException("Expected chained return to be the wrapped object but it wasn't.");
            return this;
        }

        public IEmbed Build()
        {
            return new DSharpEmbed(Wrapped.Build());
        }

        public IEmbedBuilder WithAuthor(string? name = null, string? url = null, string? iconUrl = null)
        {
            var result = Wrapped.WithAuthor(name, url, iconUrl);
            if (result != Wrapped) throw new ApplicationException("Expected chained return to be the wrapped object but it wasn't.");
            return this;
        }

        public IEmbedBuilder WithColor(IColor color)
        {
            if (color is not DSharpColor dc)
                throw new InvalidOperationException("Expected IColor that works with Discord");

            var result = Wrapped.WithColor(dc.Wrapped);
            if (result != Wrapped) throw new ApplicationException("Expected chained return to be the wrapped object but it wasn't.");
            return this;
        }

        public IEmbedBuilder WithDescription(string description)
        {
            var result = Wrapped.WithDescription(description);
            if (result != Wrapped) throw new ApplicationException("Expected chained return to be the wrapped object but it wasn't.");
            return this;
        }

        public IEmbedBuilder WithFooter(string? text = null, string? iconUrl = null)
        {
            var result = Wrapped.WithFooter(text, iconUrl);
            if (result != Wrapped) throw new ApplicationException("Expected chained return to be the wrapped object but it wasn't.");
            return this;
        }

        public IEmbedBuilder WithImageUrl(string url)
        {
            var result = Wrapped.WithImageUrl(url);
            if (result != Wrapped) throw new ApplicationException("Expected chained return to be the wrapped object but it wasn't.");
            return this;
        }

        public IEmbedBuilder WithTitle(string title)
        {
            var result = Wrapped.WithTitle(title);
            if (result != Wrapped) throw new ApplicationException("Expected chained return to be the wrapped object but it wasn't.");
            return this;
        }
    }
}
