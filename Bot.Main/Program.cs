using Bot.Core;
using Bot.DSharp;
using System.Threading.Tasks;

namespace Bot.Main
{
    public class Program
    {
        static async Task Main(string[] _)
        {
            DotEnv.Load(@"..\..\..\..\.env");

            var sp = ServiceProviderFactory.CreateServiceProvider();
            DSharpSystem dSharpSystem = new(sp);
            BotSystemRunner botRunner = new(dSharpSystem);

            await botRunner.InitializeAsync();
            await Task.Delay(-1);
        }
    }
}
