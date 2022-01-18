using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
    public struct TownDescription
    {
        IGuild Guild { get; set; }
        string TownName { get; set; }
        string? ControlChannelName { get; set; }
        string? TownSquareName { get; set; }
        string? DayCategoryName { get; set; }
        string? NightCategoryName { get; set; }
        string? StorytellerRoleName { get; set; }
        string? VillagerRoleName { get; set; }
        string? ChatChannelName { get; set; }
        IMember Author { get; set; }
    }
}
