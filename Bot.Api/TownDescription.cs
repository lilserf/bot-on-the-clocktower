using System;
using System.Collections.Generic;
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
    }
}
