using Bot.Api;
using Bot.DSharp;
using Xunit;

namespace Test.Bot.DSharp
{
    public static class TestClient
    {
        [Fact]
        public static void ConstructSystem_NoExceptions()
        {
            _ = new DSharpSystem();
        }

        [Fact]
        public static void System_ImplementsSystemInteraface()
        {
            Assert.True(typeof(IBotSystem).IsAssignableFrom(typeof(DSharpSystem)));
        }
    }
}
