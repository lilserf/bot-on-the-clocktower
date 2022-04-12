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

        public static bool TryGetObjectBoolProp(JObject obj, string propName, out bool value)
        {
            value = false;
            if (obj.TryGetValue(propName, out var token) && token != null && token.Type == JTokenType.Boolean)
            {
                value = token.Value<bool>();
                return true;
            }
            return false;
        }

        public static JArray? GetObjectArrayProp(JObject obj, string propName)
        {
            if (obj.TryGetValue(propName, out var token))
                return token as JArray;
            return null;
        }

        public static CharacterData? ParseCharacterData(JObject obj, bool isOfficial)
        {
            string? id = GetObjectStringProp(obj, "id");
            string? name = GetObjectStringProp(obj, "name");
            string? ability = GetObjectStringProp(obj, "ability");
            if (name == null || ability == null || id == null)
                return null;

            if (!Enum.TryParse<CharacterTeam>(GetObjectStringProp(obj, "team"), ignoreCase:true, out var team))
                return null;

            CharacterData cd = new(id, name, ability, team, isOfficial: isOfficial);

            cd.FlavorText = GetObjectStringProp(obj, "flavor");
            var imageUrlStr = GetObjectStringProp(obj, "image");
            if (imageUrlStr != null)
                cd.ImageUrl = Uri.EscapeUriString(imageUrlStr);

            return cd;
        }
    }
}
