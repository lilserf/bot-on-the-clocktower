using Bot.Api.Lookup;
using Newtonsoft.Json.Linq;

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

        public static CharacterData? ParseCharacterData(JObject obj)
        {
            string? name = GetObjectStringProp(obj, "name");
            string? ability = GetObjectStringProp(obj, "ability");
            string? team = GetObjectStringProp(obj, "team");
            if (name == null || ability == null || team == null)
                return null;

            return new CharacterData(name!, ability!, CharacterTeam.Townsfolk, isOfficial: false);
        }
    }
}
