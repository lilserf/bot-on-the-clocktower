namespace Bot.Api
{
    public interface IEnvironment
    {
        string? GetEnvironmentVariable(string key);
    }
}
