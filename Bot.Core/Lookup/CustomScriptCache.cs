using Bot.Api.Lookup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class CustomScriptCache : ICustomScriptCache
    {
        private static readonly GetCustomScriptResult EmptyResult = new GetCustomScriptResult(Enumerable.Empty<ScriptWithCharacters>());

        private readonly IStringDownloader m_stringDownloader;

        public CustomScriptCache(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_stringDownloader);
        }

        public async Task<GetCustomScriptResult> GetCustomScriptAsync(string url)
        {
            string? json = (await m_stringDownloader.DownloadStringAsync(url)).Data;
            if (json == null)
                return EmptyResult;

            JArray? arr;
            try
            {
                arr = JArray.Parse(json);
            }
            catch (Exception)
            {
                return EmptyResult;
            }
            if (arr == null)
                return EmptyResult;

            List<CharacterData> characters = new();
            ScriptData? scriptData = null;

            foreach (var item in arr)
            {
                if (item is not JObject obj)
                    continue;

                CharacterData? cd = null;
                if (GetObjectStringProp(obj, "id") == "_meta")
                    scriptData = ParseScriptData(obj);
                else
                    cd = ParseCharacterData(obj);

                if (cd != null)
                    characters.Add(cd);
            }

            if (scriptData == null)
                return EmptyResult;

            return new GetCustomScriptResult(new[] { new ScriptWithCharacters(scriptData, characters) });
        }

        private string? GetObjectStringProp(JObject obj, string propName)
        {
            if (obj.TryGetValue(propName, out var token) && token != null && token.Type == JTokenType.String)
                return token.Value<string>();
            return null;
        }

        private ScriptData? ParseScriptData(JObject obj)
        {
            string? name = GetObjectStringProp(obj, "name");
            if (name == null)
                return null;

            ScriptData sd = new(name, isOfficial: false);
            sd.AlmanacUrl = GetObjectStringProp(obj, "almanac");
            return sd;
        }

        private CharacterData? ParseCharacterData(JObject obj)
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
