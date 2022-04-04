using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotMessaging : IBotMessaging
    {
        private readonly ITownInteractionQueue m_townCommandQueue;
        private readonly ICommandMetricDatabase m_commandMetricsDatabase;
        private readonly IDateTime m_dateTime;

        public BotMessaging(IServiceProvider services)
        {
            services.Inject(out m_townCommandQueue);
            services.Inject(out m_commandMetricsDatabase);
            services.Inject(out m_dateTime);
        }

        private const string DemonGreeting = "{0}: You are the **demon**. ";
        private const string OtherDemons = "Your fellow demons are: {0}. ";
        private const string MinionsList = "Your minions are: {0}.";

        private static string BuildDemonMessage(IReadOnlyCollection<IMember> demons, IMember demon, IReadOnlyCollection<IMember> minions)
        {
            var greetMsg = string.Format(DemonGreeting, demon.DisplayName);
            var otherDemons = demons.Where(x => !x.Equals(demon)).Select(x => x.DisplayName);
            var otherDemonsMsg = otherDemons.Any()
                ? string.Format(OtherDemons, string.Join(", ",otherDemons)) 
                : "";
            var minionsMsg = string.Format(MinionsList, string.Join(", ", minions.Select(x => x.DisplayName)));

            return $"{greetMsg}{otherDemonsMsg}{minionsMsg}";
        }

        private const string MinionGreeting = "{0}: You are a **minion**. ";
        private const string SingleDemonList = "Your demon is: {0}. ";
        private const string MultiDemonList = "Your demons are: {0}. ";
        private const string FellowMinionsList = "Your fellow minions are: {0}";

        private static string BuildMinionMessage(IReadOnlyCollection<IMember> demons, IMember minion, IReadOnlyCollection<IMember> otherMinions)
        {
            var greetMsg = string.Format(MinionGreeting, minion.DisplayName);
            var demonMsg = demons.Count > 1 
                ? string.Format(MultiDemonList, string.Join(", ", demons.Select(x => x.DisplayName))) 
                : (demons.Count == 0 ? "" : string.Format(SingleDemonList, demons.Select(x => x.DisplayName).First()));
            var fellowMinionMsg = otherMinions.Count > 0
                ? string.Format(FellowMinionsList, string.Join(", ", otherMinions.Select(m => m.DisplayName)))
                : "";

            return $"{greetMsg}{demonMsg}{fellowMinionMsg}";
        }

        private static async Task SendDemonMessage(IReadOnlyCollection<IMember> demons, IReadOnlyCollection<IMember> minions, IProcessLogger _)
        {
            foreach (var demon in demons)
                await demon.SendMessageAsync(BuildDemonMessage(demons, demon, minions));
        }

        private static async Task SendMinionMessages(IReadOnlyCollection<IMember> demons, IReadOnlyCollection<IMember> minions, IProcessLogger _)
        {
            foreach(var m in minions)
                await m.SendMessageAsync(BuildMinionMessage(demons, m, minions.Where(x => !x.Equals(m)).ToList()));
        }

        public async Task<string> SendEvilMessage(IMember demon, IReadOnlyCollection<IMember> minions)
        {
            ProcessLogger logger = new();
            try
            {
                await SendDemonMessage(new[] { demon }, minions, logger);
                await SendMinionMessages(new[] { demon }, minions, logger);
            }
            catch(Exception ex)
            {
                logger.LogException(ex, "sending messages to the Evil team");
            }

            return "The Evil team has been informed.";
        }

        public async Task<string> SendLegionMessage(IReadOnlyCollection<IMember> legions)
        {
            ProcessLogger logger = new();
            try
            {
                await SendDemonMessage(legions, Enumerable.Empty<IMember>().ToList(), logger);
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "sending message to the Lunatic");
            }

            return "Legion has been informed.";
        }

        public async Task<string> SendLunaticMessage(IMember lunatic, IReadOnlyCollection<IMember> fakeMinions)
        {
            ProcessLogger logger = new();
            try
            {
                await SendDemonMessage(new[] { lunatic }, fakeMinions, logger);
            }
            catch(Exception ex)
            {
                logger.LogException(ex, "sending message to the Lunatic");
            }

            return "The Lunatic has been informed.";
        }

        public async Task<string> SendMagicianMessage(IMember demon, IReadOnlyCollection<IMember> minions, IMember magician)
        {
            ProcessLogger logger = new();
            try
            {
                var fakeMinions = minions.ToList();
                fakeMinions.Add(magician);
                await SendDemonMessage(new[] { demon }, fakeMinions, logger);
                await SendMinionMessages(new[] { demon, magician }, minions, logger);
            }
            catch(Exception ex)
            {
                logger.LogException(ex, "sending Magician messages");
            }

            return "The Evil team has been informed, misleadingly.";
        }

        public Task CommandEvilMessageAsync(IBotInteractionContext ctx, IMember demon, IReadOnlyCollection<IMember> minions, IMember? magician)
        {
            return m_townCommandQueue.QueueInteractionAsync("Informing...", ctx, async () =>
            {
                await m_commandMetricsDatabase.RecordCommand("evil", m_dateTime.Now);

                string msg = (magician != null)
                    ? await SendMagicianMessage(demon, minions, magician)
                    : await SendEvilMessage(demon, minions);
                return InteractionResult.FromMessage(msg);
            });
        }

        public Task CommandLunaticMessageAsync(IBotInteractionContext ctx, IMember lunatic, IReadOnlyCollection<IMember> fakeMinions)
        {
            return m_townCommandQueue.QueueInteractionAsync("Informing...", ctx, async () =>
            {
                await m_commandMetricsDatabase.RecordCommand("lunatic", m_dateTime.Now);
                string msg = await SendLunaticMessage(lunatic, fakeMinions);
                return InteractionResult.FromMessage(msg);
            });
        }

        public Task CommandLegionMessageAsync(IBotInteractionContext ctx, IReadOnlyCollection<IMember> legions)
        {
            return m_townCommandQueue.QueueInteractionAsync("Informing...", ctx, async () =>
            {
                await m_commandMetricsDatabase.RecordCommand("legion", m_dateTime.Now);
                string msg = await SendLegionMessage(legions);
                return InteractionResult.FromMessage(msg);
            });
        }
    }
}
