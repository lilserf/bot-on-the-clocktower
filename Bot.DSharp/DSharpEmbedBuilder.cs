using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpEmbedBuilder : DiscordWrapper<DiscordEmbedBuilder>, IEmbedBuilder
    {
        public DSharpEmbedBuilder(DiscordEmbedBuilder wrapped)
            : base(wrapped)
        {

        }
    }
}
