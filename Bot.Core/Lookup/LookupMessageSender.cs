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

            if (lookupItem.Scripts.Count > 0)
            {
                sb.AppendLine("Found in:");
                foreach (var script in lookupItem.Scripts)
                    AppendScriptInfo(sb, script);
            }

            if (lookupItem.Character.FlavorText != null)
                sb.AppendLine(lookupItem.Character.FlavorText);

            return channel.SendMessageAsync(sb.ToString());
        }

        private void AppendScriptInfo(StringBuilder sb, ScriptData script)
        {
            string authorSuffix = script.Author != null ? $" by {script.Author}" : "";
            string officialSuffix = script.IsOfficial ? $" (Official) - {OfficialWikiHelper.GetWikiUrl(script.Name)}" : "";
            sb.AppendLine($"⦁ {script.Name}{authorSuffix}{officialSuffix}");
        }
    }
}
