using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
    public struct TownDescription
    {
        public IGuild Guild { get; set; }
        public string TownName { get; set; }
        // Name of the control channel - if null, a default name will be used
        public string? ControlChannelName { get; set; }
        // Name of the Town Square channel - if null, a default name will be used
        public string? TownSquareName { get; set; }
        // Name of the Day category - if null, a default name will be used
        public string? DayCategoryName { get; set; }
        // Name of the Night category - if null, **no Night category or Cottages will be created**
        public string? NightCategoryName { get; set; }
        // Name of the Storyteller role - if null, a default name will be used
        public string? StorytellerRoleName { get; set; }
        // Name of the Villager role - if null, a default name will be used
        public string? VillagerRoleName { get; set; }
        // Name of the chat channel - if null, **no chat channel will be created**
        public string? ChatChannelName { get; set; }
        public IMember Author { get; set; }

        [MemberNotNull(nameof(DayCategoryName))]
        [MemberNotNull(nameof(TownSquareName))]
        [MemberNotNull(nameof(ControlChannelName))]
        [MemberNotNull(nameof(StorytellerRoleName))]
        [MemberNotNull(nameof(VillagerRoleName))]
        public void FallbackToDefaults()
        {
            if (DayCategoryName == null)
                DayCategoryName = string.Format(IBotSetup.DefaultDayCategoryFormat, TownName);
            if (TownSquareName == null)
                TownSquareName = IBotSetup.DefaultTownSquareChannelName;
            if (ControlChannelName == null)
                ControlChannelName = string.Format(IBotSetup.DefaultControlChannelFormat, TownName);
            if (StorytellerRoleName == null)
                StorytellerRoleName = string.Format(IBotSetup.DefaultStorytellerRoleFormat, TownName);
            if (VillagerRoleName == null)
                VillagerRoleName = string.Format(IBotSetup.DefaultVillagerRoleFormat, TownName);
        }

        [MemberNotNull(nameof(DayCategoryName))]
        [MemberNotNull(nameof(TownSquareName))]
        [MemberNotNull(nameof(ControlChannelName))]
        [MemberNotNull(nameof(StorytellerRoleName))]
        [MemberNotNull(nameof(VillagerRoleName))]
        [MemberNotNull(nameof(Guild))]
        [MemberNotNull(nameof(Author))]
        [MemberNotNull(nameof(TownName))]
        [MemberNotNull(nameof(ChatChannelName))]
        public void PopulateFromTownName(string townName, IGuild guild, IMember author, bool useNight=true)
        {
            Guild = guild;
            Author = author;
            TownName = townName;
            ChatChannelName = IBotSetup.DefaultChatChannelName;
            if (useNight)
                NightCategoryName = string.Format(IBotSetup.DefaultNightCategoryFormat, townName);

            FallbackToDefaults();
        }
    }
}
