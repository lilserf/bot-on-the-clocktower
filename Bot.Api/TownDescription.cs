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
        public string? ControlChannelName { get; set; }
        public string? TownSquareName { get; set; }
        public string? DayCategoryName { get; set; }
        public string? NightCategoryName { get; set; }
        public string? StorytellerRoleName { get; set; }
        public string? VillagerRoleName { get; set; }
        public string? ChatChannelName { get; set; }
        public IMember Author { get; set; }
    }
}
