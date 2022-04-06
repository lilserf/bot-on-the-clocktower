using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpRole : DiscordWrapper<DiscordRole>, IRole
    {
        public string Name => Wrapped.Name;
        public string Mention => Wrapped.Mention;
        public ulong Id => Wrapped.Id;
        public bool IsThisBot
        {
            get
            {
                var botName = Environment.GetEnvironmentVariable("BOT_NAME") ?? "Bot on the Clocktower";
                return Wrapped.IsManaged && Wrapped.Name.Equals(botName);
            }
        }

        public DSharpRole(DiscordRole wrapped)
			: base(wrapped)
		{}

        public Task DeleteAsync()
        {
            return Wrapped.DeleteAsync();
        }
    }
}
