using Bot.Api;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSystemRunner
    {
        private readonly IBotSystem mSystem;

        public BotSystemRunner(IBotSystem system)
        {
            mSystem = system;
        }

        public Task InitializeAsync()
        {
            return mSystem.InitializeAsync();
        }
    }
}
