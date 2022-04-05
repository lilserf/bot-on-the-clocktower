using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpMessageBuilder : DiscordWrapper<DiscordMessageBuilder>, IMessageBuilder
    {
        public DSharpMessageBuilder(DiscordMessageBuilder wrapped)
            : base(wrapped) { }

        public IMessageBuilder AddEmbed(IEmbed embed)
        {
            if(embed is DSharpEmbed dembed)
            {
                Wrapped.AddEmbed(dembed.Wrapped);
            }
            return this;
        }

        public IMessageBuilder AddEmbeds(IEnumerable<IEmbed> embeds)
        {
            var inners = embeds.Cast<DSharpEmbed>().Select(x => x.Wrapped);
            Wrapped.AddEmbeds(inners);
            return this;
        }

        public IMessageBuilder WithContent(string s)
        {
            Wrapped.WithContent(s);
            return this;
        }
    }
}
