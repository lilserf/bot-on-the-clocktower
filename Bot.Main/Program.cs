using Bot.Core;
using Bot.DSharp;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Main
{
    public class Program
    {
        static async Task Main(string[] _)
        {
            DotEnv.Load(@"..\..\..\..\.env");

            var sp = ServiceProviderFactory.CreateServiceProvider();
            DSharpSystem dSharpSystem = new();
            BotSystemRunner botRunner = new(sp, dSharpSystem);

            await botRunner.RunAsync(CancellationToken.None);
        }
    }
}
