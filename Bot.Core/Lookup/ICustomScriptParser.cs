namespace Bot.Core.Lookup
{
    public interface ICustomScriptParser
    {
        GetCustomScriptResult ParseScript(string json);
    }
}
