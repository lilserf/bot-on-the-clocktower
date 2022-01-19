using Bot.Api;
using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSetup : IBotSetup
    {
        public IEnumerable<string> DefaultExtraDayChannels => new[] { "Dark Alley", "Library", "Graveyard", "Pie Shop" };

        private readonly ITownDatabase m_townDb;

        public BotSetup(IServiceProvider sp)
        {
            m_townDb = sp.GetService<ITownDatabase>();
        }

        public Task AddTown(ITown town, IMember author)
        {
            return m_townDb.AddTown(town, author);
        }

        public async Task CreateTown(TownDescription townDesc)
        {
            IGuild guild = townDesc.Guild;

            if (townDesc.DayCategoryName != null)
            {
                var dayCat = await guild.CreateCategoryAsync(townDesc.DayCategoryName);

                if(townDesc.ControlChannelName != null)
                    await guild.CreateTextChannelAsync(townDesc.ControlChannelName, dayCat);
                if(townDesc.ChatChannelName != null)
                    await guild.CreateTextChannelAsync(townDesc.ChatChannelName, dayCat);
                if(townDesc.TownSquareName != null)
                    await guild.CreateVoiceChannelAsync(townDesc.TownSquareName, dayCat);
                foreach(var chanName in DefaultExtraDayChannels)
                {
                    await guild.CreateVoiceChannelAsync(chanName, dayCat);
                }
            }

            if(townDesc.NightCategoryName != null)
            {
                var nightCat = await guild.CreateCategoryAsync(townDesc.NightCategoryName);

                for(int i=0; i < IBotSetup.NumCottages; i++)
                {
                    await guild.CreateVoiceChannelAsync(IBotSetup.DefaultCottageName, nightCat);
                }
            }

            if(townDesc.StorytellerRoleName != null)
            {
                await guild.CreateRoleAsync(townDesc.StorytellerRoleName, Color.Magenta);
            }
            if(townDesc.VillagerRoleName != null)
            {
                await guild.CreateRoleAsync(townDesc.VillagerRoleName, Color.DarkMagenta);
            }
        }
    }
}
