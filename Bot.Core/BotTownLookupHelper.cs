using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    /// <summary>
    /// TODO: Should use composition, not inheritence, for this
    /// </summary>
    public abstract class BotTownLookupHelper
    {
        protected readonly IBotClient m_client;
        protected readonly ITownLookup m_townLookup;

        protected const string InvalidTownMessage = "Couldn't find a registered town for this server and channel. Consider re-creating the town with `/createTown` or `/addTown`.";

        public BotTownLookupHelper(IServiceProvider serviceProvider)
        {
            m_client = serviceProvider.GetService<IBotClient>();
            m_townLookup = serviceProvider.GetService<ITownLookup>();
        }

        protected Task<ITown?> GetValidTownOrLogErrorAsync(IBotInteractionContext context, IProcessLogger processLogger)
        {
            return GetValidTownOrLogErrorAsync(context.Guild.Id, context.Channel.Id, processLogger);
        }

        protected async Task<ITown?> GetValidTownOrLogErrorAsync(ulong guildId, ulong controlChannelId, IProcessLogger processLogger)
        {
            var townRecordList = await m_townLookup.GetTownRecords(guildId);
            var townRec = townRecordList.Where(x => x.ControlChannelId == controlChannelId).FirstOrDefault();
            
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
