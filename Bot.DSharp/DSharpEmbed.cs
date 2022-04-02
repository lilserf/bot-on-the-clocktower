using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpEmbed : DiscordWrapper<DiscordEmbed>, IEmbed
    {
        public DSharpEmbed(DiscordEmbed wrapped)
            : base(wrapped) { }
    }
}
