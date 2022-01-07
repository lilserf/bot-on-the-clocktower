using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bot.Core
{
    public static class TimeSpanParser
    {
        private static readonly Regex s_minRegex = new(@"(\d+)(?:\s*)(?:minutes|minute|mins|min|m)\s*,?");
        private static readonly Regex s_secRegex = new(@"(\d+)(?:\s*)(?:seconds|second|secs|sec|s)\s*,?");

        public static TimeSpan? Parse(string time)
        {
            (int minutes, Match? minMatch) = FindRegexMatch(s_minRegex, time);
            (int seconds, Match? secMatch) = FindRegexMatch(s_secRegex, time);

            if (minutes == 0 && seconds == 0)
                return null;

            if (!DoRegexMatchesCoverEntireString(time, minMatch, secMatch))
                return null;

            return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        private static (int, Match?) FindRegexMatch(Regex regex, string str)
        {
            int value = 0;

            var matches = regex.Matches(str);
            if (matches.Count > 1)
                return (0, null);

            var match = matches.FirstOrDefault();
            if (match != null)
                if (!int.TryParse(match.Groups[1].Value, out value))
                    return (0, null);

            return (value, match);
        }

        private static bool DoRegexMatchesCoverEntireString(string str, params Match?[] matches)
        {
            foreach (var match in matches)
                if (match != null)
                    str = str.Remove(str.IndexOf(match.Value), match.Length);

            return string.IsNullOrWhiteSpace(str);
        }
    }
}
