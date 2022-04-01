using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IAnnouncementDatabase
    {
        public Task<bool> HasSeenVersion(ulong guildId, Version version);

        public Task RecordGuildHasSeenVersion(ulong guildId, Version version, bool force = false);
    }
}
