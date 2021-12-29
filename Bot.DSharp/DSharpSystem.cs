using Bot.Api;
using System;

namespace Bot.DSharp
{
    public class DSharpSystem : IBotSystem
    {
        public IBotClient CreateClient(IServiceProvider serviceProvider) => new DSharpClient(serviceProvider);
    }
}
