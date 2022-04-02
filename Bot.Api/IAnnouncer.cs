using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IAnnouncer
    {
        public Task AnnounceLatestVersion();

        public Task SetGuildAnnounce(ulong guildId, bool announce);

        public Task CommandSetGuildAnnounce(IBotInteractionContext ctx, bool hear);
    }
}
