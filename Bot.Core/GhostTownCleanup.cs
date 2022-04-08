﻿using Bot.Api;
using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    internal class GhostTownCleanup
    {
        IStartupTownTasks m_startupTownTasks;
        IBotClient m_botClient;
        ITownDatabase m_townDatabase;

        public GhostTownCleanup(IServiceProvider sp)
        {
            sp.Inject(out m_startupTownTasks);
            sp.Inject(out m_botClient);
            sp.Inject(out m_townDatabase);

            m_startupTownTasks.AddStartupTask(CleanupGhostTown);
        }

        private async Task CleanupGhostTown(TownKey townKey)
        {
            Serilog.Log.Information("GhostTownCleanup: Checking town {townKey}", townKey);
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
