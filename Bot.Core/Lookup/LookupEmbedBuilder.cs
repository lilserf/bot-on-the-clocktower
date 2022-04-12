using Bot.Api;
using System;
using System.Linq;
using System.Text;

namespace Bot.Core.Lookup
{
    public class LookupEmbedBuilder : ILookupEmbedBuilder
    {
        private readonly IBotSystem m_botSystem;

        public LookupEmbedBuilder(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_botSystem);
        }

        public IEmbed BuildLookupEmbed(LookupCharacterItem lookupItem)
        {
            string name = lookupItem.Character.Name;

            var eb = m_botSystem.CreateEmbedBuilder()
                .WithTitle(name)
                .WithDescription(Enum.GetName(lookupItem.Character.Team)!)
                .WithColor(CharacterColorHelper.GetColorForTeam(m_botSystem.ColorBuilder, lookupItem.Character.Team))
                .AddField("Ability", lookupItem.Character.Ability);

            bool isOfficialCharWithoutOfficialScript = lookupItem.Character.IsOfficial && lookupItem.Scripts.All(s => !s.IsOfficial);
            bool needsFoundInEmbed = isOfficialCharWithoutOfficialScript || lookupItem.Scripts.Count > 0;

            if (needsFoundInEmbed)
            {
                var sb = new StringBuilder();

                if (isOfficialCharWithoutOfficialScript)
                {
                    string nameAsWikiLink = lookupItem.Character.IsOfficial ? $"[{name}]({OfficialWikiHelper.GetWikiUrl(name)})" : name;
                    sb.AppendLine($"Official Wiki - {nameAsWikiLink}");
                }

                foreach (var script in lookupItem.Scripts)
                    AppendScriptInfo(sb, lookupItem.Character, script);

                eb.AddField("Found In", sb.ToString());
            }

            if (lookupItem.Character.ImageUrl != null)
                eb.WithThumbnail(lookupItem.Character.ImageUrl);

            if (lookupItem.Character.FlavorText != null)
                eb.WithFooter(lookupItem.Character.FlavorText);

            return eb.Build();
        }

        private void AppendScriptInfo(StringBuilder sb, CharacterData character, ScriptData script)
        {
            string? wikiLink = script.IsOfficial ? OfficialWikiHelper.GetWikiUrl(script.Name) : script.AlmanacUrl;
            string nameWithLink = wikiLink != null ? $"[{script.Name}]({wikiLink})" : script.Name;
            string officialSuffix = script.IsOfficial ? $" (Official)" : "";
            string authorSuffix = script.Author != null ? $" by {script.Author}" : "";
            string wikiSuffix = script.IsOfficial && character.IsOfficial ? $" - [{character.Name}]({OfficialWikiHelper.GetWikiUrl(character.Name)})" : GetCustomWikiSuffixForAlmanac(character, script);
            sb.AppendLine($"{nameWithLink}{authorSuffix}{officialSuffix}{wikiSuffix}");
        }

        private string GetCustomWikiSuffixForAlmanac(CharacterData character, ScriptData script)
        {
            if (!script.IsOfficial && script.AlmanacUrl != null && script.AlmanacUrl.StartsWith("https://www.bloodstar.xyz/"))
                return $" - [{character.Name}]({script.AlmanacUrl}#{character.Id})";
            return "";
        }
    }
}
