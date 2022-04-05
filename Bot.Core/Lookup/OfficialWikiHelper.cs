using System.Linq;
using System.Web;

namespace Bot.Core.Lookup
{
    public static class OfficialWikiHelper
    {
        public const string WikiPrefixUrl = "https://wiki.bloodontheclocktower.com/";

        public static string GetWikiUrl(string thingName) => $"{WikiPrefixUrl}{HttpUtility.UrlEncode(string.Join('_', thingName.Split(' ').Select(s => $"{char.ToUpper(s[0])}{s[1..]}")))}";
    }
}
