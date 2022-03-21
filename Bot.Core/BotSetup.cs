using Bot.Api;
using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSetup : IBotSetup
    {
        public IEnumerable<string> DefaultExtraDayChannels => new[] { "Dark Alley", "Library", "Graveyard", "Pie Shop" };

        private readonly ITownDatabase m_townDb;
        private readonly IBotSystem m_botSystem;
        private readonly IComponentService m_componentService;

        IBotComponent m_townNameText;
        IBotComponent m_controlChannelText;
        IBotComponent m_townSquareText;
        IBotComponent m_dayCategoryText;
        IBotComponent m_nightCategoryText;
        IBotComponent m_storytellerRoleText;
        IBotComponent m_villagerRoleText;

        public BotSetup(IServiceProvider sp)
        {
            sp.Inject(out m_townDb);
            sp.Inject(out m_botSystem);
            sp.Inject(out m_componentService);

            m_townNameText = m_botSystem.CreateTextInput("text-town-name", "Town Name", "Ravenswood Bluff", "Ravenswood Bluff");
            m_controlChannelText = m_botSystem.CreateTextInput("text-control-channel", "Control Channel Name", "botc-control");
            m_townSquareText = m_botSystem.CreateTextInput("text-town-square", "Town Square Channel Name", "Town Square");
            m_dayCategoryText = m_botSystem.CreateTextInput("text-day-category", "Day Category Name", "Ravenswood Bluff - Day");
            m_nightCategoryText = m_botSystem.CreateTextInput("text-night-category", "Night Category Name", "Ravenswood Bluff - Night");
        }

        public async Task CommandCreateTown(IBotInteractionContext ctx)
        {
            var builder = m_botSystem.CreateInteractionResponseBuilder().WithTitle("Create A Town").WithCustomId("create-town");
            builder.AddComponents(m_townNameText);
            builder.AddComponents(m_controlChannelText);
            builder.AddComponents(m_townSquareText);
            builder.AddComponents(m_dayCategoryText);
            builder.AddComponents(m_nightCategoryText);
            // Modals can only have 5 inputs to we just don't ask about role names

            await ctx.ShowModalAsync(builder);
        }

        public Task AddTown(ITown town, IMember author)
        {
            return m_townDb.AddTownAsync(town, author);
        }

        private static TownDescription FallbackToDefaults(TownDescription desc)
        {
            if (desc.DayCategoryName == null)
                desc.DayCategoryName = string.Format(IBotSetup.DefaultDayCategoryFormat, desc.TownName);
            if (desc.TownSquareName == null)
                desc.TownSquareName = IBotSetup.DefaultTownSquareChannelName;
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
