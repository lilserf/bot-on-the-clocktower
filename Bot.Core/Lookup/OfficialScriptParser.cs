using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bot.Core.Lookup
{
    public class OfficialScriptParser : IOfficialScriptParser
    {
        public const string AlmanacPrefix = "https://wiki.bloodontheclocktower.com/";

        public GetOfficialCharactersResult ParseOfficialData(IEnumerable<string> scriptJsons, IEnumerable<string> characterJsons)
        {
            Dictionary<string, ScriptData> scriptIdToScriptMap = new();
            List<ScriptData> scripts = new();
            Dictionary<ScriptData, IReadOnlyCollection<string>> scriptToCharactersMap = new();

            foreach (var scriptsJson in scriptJsons)
                ParseScripts(scriptsJson, scripts, scriptIdToScriptMap, scriptToCharactersMap);

            Dictionary<string, CharacterDataWithScript> charIdToCharMap = new();
            List<CharacterDataWithScript> charList = new();

            foreach (var characterJson in characterJsons)
                ParseCharacters(characterJson, charIdToCharMap, charList, scriptIdToScriptMap);

            foreach (var script in scripts)
                if (scriptToCharactersMap.TryGetValue(script, out var charsInScript))
                    foreach (var cid in charsInScript)
                        if (charIdToCharMap.TryGetValue(cid, out var cdws))
                            cdws.Scripts.Add(script);

            List<GetOfficialCharactersItem> retItems = new();

            foreach (var cd in charList)
                retItems.Add(new GetOfficialCharactersItem(cd.CharacterData, cd.Scripts));

            return new GetOfficialCharactersResult(retItems);
        }

        private void ParseScripts(string scriptsJson, ICollection<ScriptData> scripts, Dictionary<string, ScriptData> scriptIdToScriptMap, Dictionary<ScriptData, IReadOnlyCollection<string>> scriptCharactersMap)
        {
            JArray? arr;
            try
            {
                arr = JArray.Parse(scriptsJson);
            }
            catch (Exception)
            {
                return;
            }
            if (arr == null)
                return;

            foreach (var item in arr)
            {
                if (item is not JObject obj)
                    continue;

                var name = JsonParseUtil.GetObjectStringProp(obj, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                bool isOfficial = true;
                if (JsonParseUtil.TryGetObjectBoolProp(obj, "isOfficial", out bool off))
                    isOfficial = off;

                ScriptData sd = new(name, isOfficial: isOfficial);
                scripts.Add(sd);

                var id = JsonParseUtil.GetObjectStringProp(obj, "id");
                if (!string.IsNullOrWhiteSpace(id))
                    scriptIdToScriptMap[id] = sd;

                var roleArray = JsonParseUtil.GetObjectArrayProp(obj, "roles");
                if (roleArray != null)
                    if (roleArray.Count <= 0)
                        sd.AlmanacUrl = GetAlmanacUrl(name); // NOTE: Almanac only valid for official scripts with a roles list that is empty. Weird, but that's how it's set up
                    else
                        scriptCharactersMap[sd] = roleArray.Where(t => t != null && t.Type == JTokenType.String).Select(t => t.Value<string>()!).ToArray();

                sd.Author = JsonParseUtil.GetObjectStringProp(obj, "author");
            }
        }

        private void ParseCharacters(string characterJson, IDictionary<string, CharacterDataWithScript> charIdToCharMap, ICollection<CharacterDataWithScript> charList, IDictionary<string, ScriptData> scriptIdToScriptMap)
        {
            JArray? arr;
            try
            {
                arr = JArray.Parse(characterJson);
            }
            catch (Exception)
            {
                return;
            }
            if (arr == null)
                return;

            foreach(var item in arr)
            {
                if (item is not JObject obj)
                    continue;

                CharacterData? cd = JsonParseUtil.ParseCharacterData(obj, isOfficial: true);
                if (cd == null)
                    continue;

                var cdws = new CharacterDataWithScript(cd);
                charList.Add(cdws);

                var id = JsonParseUtil.GetObjectStringProp(obj, "id");
                if (!string.IsNullOrWhiteSpace(id))
                    charIdToCharMap[id] = cdws; // NOTE: Not great support for duped ids. Not even sure what I would do. Expected no duped ids

                var scriptId = JsonParseUtil.GetObjectStringProp(obj, "edition");
                if (!string.IsNullOrWhiteSpace(scriptId))
                    if (scriptIdToScriptMap.TryGetValue(scriptId, out var script))
                        cdws.Scripts.Add(script);
            }
        }

        private static string GetAlmanacUrl(string scriptName) => $"{AlmanacPrefix}{HttpUtility.UrlEncode(string.Join('_', scriptName.Split(' ').Select(s => $"{char.ToUpper(s[0])}{s[1..]}")))}";

        private class CharacterDataWithScript
        {
            public CharacterData CharacterData { get; }
            public ICollection<ScriptData> Scripts { get; } = new List<ScriptData>();
            public CharacterDataWithScript(CharacterData cd)
            {
                CharacterData = cd;
            }
        }
    }
}
