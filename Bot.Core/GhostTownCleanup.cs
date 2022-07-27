using Bot.Api;
using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    internal class GhostTownCleanup : IGhostTownCleanup
    {
        private readonly ITownMaintenance m_startupTownTasks;
        private readonly IBotClient m_botClient;
        private readonly ITownDatabase m_townDatabase;
        private readonly IGameMetricDatabase m_gameMetricDatabase;
        private readonly IDateTime m_dateTime;

        public GhostTownCleanup(IServiceProvider sp)
        {
            sp.Inject(out m_startupTownTasks);
            sp.Inject(out m_botClient);
            sp.Inject(out m_townDatabase);
            sp.Inject(out m_gameMetricDatabase);
            sp.Inject(out m_dateTime);

            m_startupTownTasks.AddMaintenanceTask(CleanupGhostTown);
        }

        private async Task CleanupGhostTown(TownKey townKey)
        {
            Serilog.Log.Verbose("GhostTownCleanup: Checking town {townKey}", townKey);

            // If there was activity in the last 30 days, skip this town
            var mostRecent = await m_gameMetricDatabase.GetMostRecentGameAsync(townKey);
            if (mostRecent != null && mostRecent.Value.AddDays(30) > m_dateTime.Now)
                return;
            
            IGuild? guild = await m_botClient.GetGuildAsync(townKey.GuildId);
            IChannel? controlChan = null;

            if(guild != null)
            {
                controlChan = guild.GetChannel(townKey.ControlChannelId);
            }

            if(guild == null || controlChan == null)
            {
                Serilog.Log.Information("GhostTownCleanup: Deleting dead town {townKey}! Bad guild: {guildMissing}, Bad chan: {controlChanMissing}", townKey, guild==null, controlChan==null);
                // This town is a ghooooost
                await m_townDatabase.DeleteTownAsync(townKey);
            }

        }
    }
}
