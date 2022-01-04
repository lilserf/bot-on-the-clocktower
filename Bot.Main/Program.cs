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

            var sp = RegisterServices();
            sp = Database.ServiceFactory.RegisterServices(sp);

            DatabaseFactory dbp = new(sp);
            sp = dbp.Connect();

            sp = Core.ServiceFactory.RegisterCoreServices(sp);

            var dsharpRunner = new BotSystemRunner(sp, new DSharpSystem());
            await dsharpRunner.RunAsync(CancellationToken.None);
        }

        public static IServiceProvider RegisterServices()
        {
            var sp = new ServiceProvider();
            sp.AddService<IEnvironment>(new ProgramEnvironment());
            return sp;
        }
    }
}
