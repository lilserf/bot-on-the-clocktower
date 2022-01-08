using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public abstract class BotCommandHandler
    {
        protected readonly IBotClient m_client;
        protected readonly IBotSystem m_system;
        protected readonly ITownLookup m_townLookup;

        protected const string InvalidTownMessage = "Couldn't find a registered town for this server and channel. Consider re-creating the town with `/createTown` or `/addTown`.";

        public BotCommandHandler(IServiceProvider serviceProvider)
        {
            m_client = serviceProvider.GetService<IBotClient>();
            m_system = serviceProvider.GetService<IBotSystem>();
            m_townLookup = serviceProvider.GetService<ITownLookup>();
        }

        // Helper for editing the original interaction with a summarizing message when finished
        // TODO: move within IBotInteractionContext
        protected async Task EditOriginalMessage(IBotInteractionContext context, string s)
        {
            try
            {
                var webhook = m_system.CreateWebhookBuilder().WithContent(s);
                await context.EditResponseAsync(webhook);
            }
            catch (Exception)
            { }
        }

        protected async Task<ITown?> GetValidTownOrLogErrorAsync(IBotInteractionContext context, IProcessLogger processLogger)
        {
            var townRecordList = await m_townLookup.GetTownRecords(context.Guild.Id);
            var townRec = townRecordList.Where(x => x.ControlChannelId == context.Channel.Id).FirstOrDefault();
            
            if (townRec == null)
            {
                if (townRecordList.Count() == 0)
                {
                    processLogger.LogMessage(InvalidTownMessage);
                }
                else
                {
                    var channels = townRecordList.Select(x => $"<#{x.ControlChannelId}>");
                    var message = string.Join(", ", channels);
                    processLogger.LogMessage($"This channel isn't a valid control channel for a town. Did you mean to run this command in one of these channels?\n{message}");
                }
                return null;
            }

            var town = await m_client.ResolveTownAsync(townRec);
            if (town == null)
            {
                processLogger.LogMessage(InvalidTownMessage);
                return null;
            }

            return town;
        }
    }
}
