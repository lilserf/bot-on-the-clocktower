using Bot.Api;
using Bot.Base;
using Bot.Core;
using Bot.Database;
using Bot.DSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Main
{
    public class Program
    {
        static async Task Main(string[] _)
        {
            DotEnv.Load(@"..\..\..\..\.env");

            // NOTE: the IEnvironment should probably be here in Bot.Main rather than in Bot.Core, and
            // we should probably do the DB services before the Bot.Core services.
            var sp = ServiceProviderFactory.CreateServiceProvider();

            sp = Bot.Database.ServiceFactory.RegisterServices(sp);

            DatabaseFactory dbp = new(sp);
            sp = dbp.Connect();

            //// TEST CODE
            //// Demonstrates that we can successfully get a Town's details from the ITownLookup service
            //ITownLookup itl = sp.GetService<ITownLookup>();
            //Town t = await itl.GetTown(128585855097896963, 826858511438839879);
            //Console.WriteLine(t);
            //// END TEST CODE

            DSharpSystem dSharpSystem = new();
            BotSystemRunner botRunner = new(sp, dSharpSystem);

            await botRunner.RunAsync(CancellationToken.None);
        }
    }
}
