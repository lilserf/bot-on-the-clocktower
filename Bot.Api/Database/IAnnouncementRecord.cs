using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IAnnouncementRecord
    {
        static Version ImpossiblyLargeVersion = new Version(999999, 0, 0);

        ulong GuildId { get; set; }
        Version Version { get; set; }
    }
}
