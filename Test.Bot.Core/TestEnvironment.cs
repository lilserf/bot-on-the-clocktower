using Bot.Core;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public static class TestEnvironment
    {
        [Fact]
        public static void Environment_NoKey_GetsNothing()
        {
            BotEnvironment env = new();
            var result = env.GetEnvironmentVariable("test_env_value");
            Assert.Null(result);
        }

        [Fact]
        public static void Environment_HasKey_GetsResult()
        {
            Environment.SetEnvironmentVariable("test_env_value", "blah");

            BotEnvironment env = new();
            var result = env.GetEnvironmentVariable("test_env_value");
            Assert.Equal("blah", result);
        }
    }
}
