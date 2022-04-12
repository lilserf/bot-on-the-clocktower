using System;
using System.Linq;

namespace Bot.Core.Lookup
{
    public static class OfficialWikiHelper
    {
        public const string WikiPrefixUrl = "https://wiki.bloodontheclocktower.com/";

        public static string GetWikiUrl(string thingName) => $"{WikiPrefixUrl}{Uri.EscapeUriString(string.Join('_', thingName.Split(' ').Select(s => $"{char.ToUpper(s[0])}{s[1..]}")))}";
    }
}
