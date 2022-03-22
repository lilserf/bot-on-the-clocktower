namespace Bot.Api.Lookup
{
    public class ScriptData
    {
        public string Name { get; }
        public bool IsOfficial { get; }
        public string? AlmanacUrl { get; set; }
        public ScriptData(string name, bool isOfficial)
        {
            Name = name;
            IsOfficial = isOfficial;
        }
    }
}
