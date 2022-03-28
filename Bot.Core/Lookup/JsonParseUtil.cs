using Bot.Api.Lookup;
using Newtonsoft.Json.Linq;
using System;

namespace Bot.Core.Lookup
{
    public static class JsonParseUtil
    {
        public static string? GetObjectStringProp(JObject obj, string propName)
        {
            if (obj.TryGetValue(propName, out var token) && token != null && token.Type == JTokenType.String)
                return token.Value<string>();
            return null;
        }

        public static CharacterData? ParseCharacterData(JObject obj, bool isOfficial)
        {
            string? name = GetObjectStringProp(obj, "name");
            string? ability = GetObjectStringProp(obj, "ability");
            if (name == null || ability == null)
                return null;

            if (!Enum.TryParse<CharacterTeam>(GetObjectStringProp(obj, "team"), ignoreCase:true, out var team))
                return null;

            CharacterData cd = new(name, ability, team, isOfficial: isOfficial);


            cd.FlavorText = GetObjectStringProp(obj, "flavor");
            cd.ImageUrl = GetObjectStringProp(obj, "image");

            return cd;
        }
    }
}
