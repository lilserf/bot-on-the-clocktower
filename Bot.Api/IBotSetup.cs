using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotSetup
    {
        public const string DefaultDayCategoryFormat = "{0}";
        public const string DefaultNightCategoryFormat = "{0} - Night";
        public const string DefaultStorytellerRoleFormat = "{0} Storyteller";
        public const string DefaultVillagerRoleFormat = "{0} Villager";
        public const string DefaultControlChannelFormat = "{0} Control";
        public const string DefaultChatChannelName = "chat";
        public const string DefaultTownSquareChannelName = "Town Square";
        public const string DefaultCottageName = "Cottage";
        public const int NumCottages = 20;

        public IEnumerable<string> DefaultExtraDayChannels { get; }

        public Task AddTown(ITown town, IMember author);

        public Task CreateTown(TownDescription townDesc);

        public Task CommandCreateTown(IBotInteractionContext ctx);
    }
}
