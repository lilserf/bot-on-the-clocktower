using Bot.Api.Lookup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Bot.Core.Lookup
{
    public class CustomScriptParser : ICustomScriptParser
    {
        private static readonly GetCustomScriptResult EmptyResult = GetCustomScriptResult.EmptyResult;

        public GetCustomScriptResult ParseScript(string json)
        {
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
                if (JsonParseUtil.GetObjectStringProp(obj, "id") == "_meta")
                    scriptData = ParseScriptMetaData(obj);
                else
                    cd = JsonParseUtil.ParseCharacterData(obj, isOfficial:false);

                if (cd != null)
                    characters.Add(cd);
            }

            if (scriptData == null)
                return EmptyResult;

            return new GetCustomScriptResult(new[] { new ScriptWithCharacters(scriptData, characters) });
        }

        private ScriptData? ParseScriptMetaData(JObject obj)
        {
            string? name = JsonParseUtil.GetObjectStringProp(obj, "name");
            if (name == null)
                return null;

            ScriptData sd = new(name, isOfficial: false);
            sd.AlmanacUrl = JsonParseUtil.GetObjectStringProp(obj, "almanac");
            sd.Author = JsonParseUtil.GetObjectStringProp(obj, "author");
            return sd;
        }
    }
}
