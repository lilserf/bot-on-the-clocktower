using Bot.Api;
using System;

namespace Bot.Core
{
    public class BotEnvironment : IEnvironment
    {
        public string? GetEnvironmentVariable(string key) => Environment.GetEnvironmentVariable(key);
    }
}
