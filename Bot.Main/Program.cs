using Bot.Core;
using Bot.Database;
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

            DatabaseFactory dbp = new(sp);
            sp = dbp.Connect();

            DSharpSystem dSharpSystem = new();
            BotSystemRunner botRunner = new(sp, dSharpSystem);

            await botRunner.RunAsync(CancellationToken.None);
        }
    }
}
