using Bot.Api;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class LookupMessageSender : ILookupMessageSender
    {
        public LookupMessageSender(IServiceProvider serviceProvider)
        {
        }

        public Task SendLookupMessageAsync(IChannel channel, LookupCharacterItem lookupItem)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{lookupItem.Character.Name} - {Enum.GetName(lookupItem.Character.Team)}");
            sb.AppendLine(lookupItem.Character.Ability);

            if (lookupItem.Character.IsOfficial)
                sb.AppendLine($"(Official) Wiki: {OfficialWikiHelper.GetWikiUrl(lookupItem.Character.Name)}");

            if (lookupItem.Character.FlavorText != null)
                sb.AppendLine(lookupItem.Character.FlavorText);

            return channel.SendMessageAsync(sb.ToString());
        }
    }
}
