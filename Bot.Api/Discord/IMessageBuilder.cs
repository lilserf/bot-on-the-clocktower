using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IMessageBuilder
    {

        IMessageBuilder AddEmbed(IEmbed embed);
        IMessageBuilder AddEmbeds(IEnumerable<IEmbed> embeds);
        IMessageBuilder WithContent(string s);

    }
}
