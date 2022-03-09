using Bot.Api;
using Bot.Base;
using Bot.Core;
using Bot.Database;
using Bot.DSharp;
using Destructurama;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Main
{
    public class Program
    {
        static async Task Main(string[] _)
        {
            // Setup the global Serilog logger instance; if we want more granularity or not to use a single static logger, we can
            // instantiate individual loggers and put them into our services instead
            Log.Logger = new LoggerConfiguration()
                .Destructure.ByIgnoringProperties<DSharpMember>(x => x.Wrapped, x => x.Roles, x => x.IsBot)
                .Destructure.ByIgnoringProperties<DSharpRole>(x => x.Wrapped, x => x.Mention)
                .Destructure.ByIgnoringProperties<DSharpGuild>(x => x.Wrapped, x => x.Roles, x => x.Members)
                .Destructure.ByIgnoringProperties<DSharpChannel>(x => x.Wrapped, x => x.Users)
                .Destructure.ByIgnoringProperties<DSharpChannelCategory>(x => x.Wrapped, x => x.Channels)
                .Destructure.ByIgnoringProperties<DSharpInteractionContext>(x => x.Wrapped, x => x.Services)
                .Destructure.ByIgnoringProperties<DSharpComponentContext>(x => x.Wrapped)
                .Destructure.ByTransforming<MongoTownRecord>(x => new { GuildId = x.GuildId, ControlChannelId = x.ControlChannelId, ControlChannelName = x.ControlChannel })
                .Destructure.ByTransforming<Town>(x => new { Guild = x.Guild, ControlChannel = x.ControlChannel })
                .Destructure.ByTransforming<Game>(x => new { TownKey = x.TownKey, Storytellers = x.Storytellers, VillagerCount = x.Villagers.Count })
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/botc.log", rollingInterval:RollingInterval.Day)
                .CreateLogger();

            DotEnv.Load(@"..\..\..\..\.env");

            var sp = RegisterServices();
            sp = Database.ServiceFactory.RegisterServices(sp);

            DatabaseFactory dbp = new(sp);
            sp = dbp.Connect();

            sp = Core.ServiceFactory.RegisterCoreServices(sp);

            sp = DSharp.ServiceFactory.RegisterServices(sp);
            var dsharpRunner = new BotSystemRunner(sp, new DSharpSystem());
            await dsharpRunner.RunAsync(CancellationToken.None);
        }

        public static IServiceProvider RegisterServices()
        {
            var sp = new ServiceProvider();
            sp.AddService<IDateTime>(new DateTimeStatic());
            sp.AddService<IEnvironment>(new ProgramEnvironment());
            sp.AddService<ITask>(new TaskStatic());
            return sp;
        }
    }
}
