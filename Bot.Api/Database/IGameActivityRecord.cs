using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IGameActivityRecord
    {
        ulong GuildId { get; }
        ulong ChannelId { get; }
        DateTime LastActivity { get; }
    }
}
