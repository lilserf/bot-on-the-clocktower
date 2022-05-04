using Bot.Api;
using System;

namespace Bot.Main
{
    public class ProgramEnvironment : IEnvironment
    {
        public string? GetEnvironmentVariable(string key) => Environment.GetEnvironmentVariable(key);
    }
}
