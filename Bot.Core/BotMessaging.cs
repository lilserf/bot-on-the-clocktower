using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotMessaging : IBotMessaging
    {
        private readonly IBotSystem m_system;
        public BotMessaging(IServiceProvider services)
        {
            services.Inject(out m_system);
        }

        private const string DemonGreeting = "{0}: You are the **demon**. ";
        private const string OtherDemons = "Your fellow demons are: {0}. ";
        private const string MinionsList = "Your minions are: {0}.";

        private string BuildDemonMessage(IReadOnlyCollection<IMember> demons, IMember demon, IReadOnlyCollection<IMember> minions)
        {
            var greetMsg = string.Format(DemonGreeting, demon.DisplayName);
            var otherDemons = demons.Where(x => !x.Equals(demon)).Select(x => x.DisplayName);
            var otherDemonsMsg = otherDemons.Count() > 0 
                ? string.Format(OtherDemons, string.Join(", ",otherDemons)) 
                : "";
            var minionsMsg = string.Format(MinionsList, string.Join(", ", minions.Select(x => x.DisplayName)));

            return $"{greetMsg}{otherDemonsMsg}{minionsMsg}";
        }

        private const string MinionGreeting = "{0}: You are a **minion**. ";
        private const string SingleDemonList = "Your demon is: {0}. ";
        private const string MultiDemonList = "Your demons are: {0}. ";
        private const string FellowMinionsList = "Your fellow minions are: {0}";

        private string BuildMinionMessage(IReadOnlyCollection<IMember> demons, IMember minion, IReadOnlyCollection<IMember> otherMinions)
        {
            var greetMsg = string.Format(MinionGreeting, minion.DisplayName);
            var demonMsg = demons.Count() > 1 
                ? string.Format(MultiDemonList, string.Join(", ", demons.Select(x => x.DisplayName))) 
                : (demons.Count() == 0 ? "" : string.Format(SingleDemonList, demons.Select(x => x.DisplayName).First()));
            var fellowMinionMsg = otherMinions.Count() > 0
                ? string.Format(FellowMinionsList, string.Join(", ", otherMinions.Select(m => m.DisplayName)))
                : "";

            return $"{greetMsg}{demonMsg}{fellowMinionMsg}";
        }

        private async Task SendDemonMessage(IReadOnlyCollection<IMember> demons, IReadOnlyCollection<IMember> minions, IProcessLogger logger)
        {
            foreach (var demon in demons)
            {
                await demon.SendMessageAsync(BuildDemonMessage(demons, demon, minions));
            }
        }

        private async Task SendMinionMessages(IReadOnlyCollection<IMember> demons, IReadOnlyCollection<IMember> minions, IProcessLogger logger)
        {
            foreach(var m in minions)
            {
                await m.SendMessageAsync(BuildMinionMessage(demons, m, minions.Where(x => !x.Equals(m)).ToList()));
            }
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

        public async Task CommandEvilMessageAsync(IBotInteractionContext ctx, IMember demon, IReadOnlyCollection<IMember> minions, IMember? magician)
        {
            await ctx.DeferInteractionResponse();

            string msg = "Unknown error.";
            if(magician != null)
            {
                msg = await SendMagicianMessage(demon, minions, magician);
            }
            else
            {
                msg = await SendEvilMessage(demon, minions);
            }

            var builder = m_system.CreateWebhookBuilder().WithContent(msg);
            await ctx.EditResponseAsync(builder);
        }

        public async Task CommandLunaticMessageAsync(IBotInteractionContext ctx, IMember lunatic, IReadOnlyCollection<IMember> fakeMinions)
        {
            await ctx.DeferInteractionResponse();

            string msg = "Unknown error.";
            msg = await SendLunaticMessage(lunatic, fakeMinions);

            var builder = m_system.CreateWebhookBuilder().WithContent(msg);
            await ctx.EditResponseAsync(builder);
        }

        public async Task CommandLegionMessageAsync(IBotInteractionContext ctx, IReadOnlyCollection<IMember> legions)
        {
            await ctx.DeferInteractionResponse();

            string msg = "Unknown error.";
            msg = await SendLegionMessage(legions);

            var builder = m_system.CreateWebhookBuilder().WithContent(msg);
            await ctx.EditResponseAsync(builder);
        }
    }
}
