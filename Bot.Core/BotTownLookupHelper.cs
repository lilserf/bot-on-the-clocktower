using Bot.Api;
using Bot.Api.Database;
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
        protected readonly ITownDatabase m_townLookup;
        protected readonly ITownResolver m_townResolver;

        protected const string InvalidTownMessage = "Couldn't find a registered town for this server and channel. Consider re-creating the town with `/createTown` or `/addTown`.";

        public BotTownLookupHelper(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_townLookup);
            serviceProvider.Inject(out m_townResolver);
        }

        protected Task<ITown?> GetValidTownOrLogErrorAsync(TownKey townKey, IProcessLogger processLogger)
        {
            return GetValidTownOrLogErrorAsync(townKey.GuildId, townKey.ControlChannelId, processLogger);
        }

        protected async Task<ITown?> GetValidTownOrLogErrorAsync(ulong guildId, ulong controlChannelId, IProcessLogger processLogger)
        {
            var townRecordList = await m_townLookup.GetTownRecordsAsync(guildId);
            var townRec = townRecordList.Where(x => x.ControlChannelId == controlChannelId).FirstOrDefault();
            
            if (townRec == null)
            {
                if (!townRecordList.Any())
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

            var town = await m_townResolver.ResolveTownAsync(townRec);
            if (town == null)
                processLogger.LogMessage(InvalidTownMessage);

            return town;
        }
    }
}
