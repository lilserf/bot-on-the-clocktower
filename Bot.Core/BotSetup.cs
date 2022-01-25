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
            sp.Inject(out m_townDb);
        }

        public Task AddTown(ITown town, IMember author)
        {
            return m_townDb.AddTown(town, author);
        }

        private TownDescription FallbackToDefaults(TownDescription desc)
        {
            if (desc.DayCategoryName == null)
                desc.DayCategoryName = string.Format(IBotSetup.DefaultDayCategoryFormat, desc.TownName);
            if (desc.TownSquareName == null)
                desc.TownSquareName = string.Format(IBotSetup.DefaultTownSquareChannelFormat, desc.TownName);
            if (desc.ControlChannelName == null)
                desc.ControlChannelName = string.Format(IBotSetup.DefaultControlChannelFormat, desc.TownName);
            if (desc.StorytellerRoleName == null)
                desc.StorytellerRoleName = string.Format(IBotSetup.DefaultStorytellerRoleFormat, desc.TownName);
            if (desc.VillagerRoleName == null)
                desc.VillagerRoleName = string.Format(IBotSetup.DefaultVillagerRoleFormat, desc.TownName);

            return desc;
        }

        public async Task CreateTown(TownDescription townDesc)
        {
            IGuild guild = townDesc.Guild;

            townDesc = FallbackToDefaults(townDesc);

            var dayCat = await guild.CreateCategoryAsync(townDesc.DayCategoryName!);
            await guild.CreateTextChannelAsync(townDesc.ControlChannelName!, dayCat);
            await guild.CreateVoiceChannelAsync(townDesc.TownSquareName!, dayCat);

            // Chat channel is optional
            if (townDesc.ChatChannelName != null)
                await guild.CreateTextChannelAsync(townDesc.ChatChannelName, dayCat);
            
            foreach(var chanName in DefaultExtraDayChannels)
            {
                await guild.CreateVoiceChannelAsync(chanName, dayCat);
            }

            // Night category is optional
            if(townDesc.NightCategoryName != null)
            {
                var nightCat = await guild.CreateCategoryAsync(townDesc.NightCategoryName);

                for(int i=0; i < IBotSetup.NumCottages; i++)
                {
                    await guild.CreateVoiceChannelAsync(IBotSetup.DefaultCottageName, nightCat);
                }
            }

            await guild.CreateRoleAsync(townDesc.StorytellerRoleName!, Color.Magenta);
            await guild.CreateRoleAsync(townDesc.VillagerRoleName!, Color.DarkMagenta);
        }
    }
}
