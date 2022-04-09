using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IAnnouncer
    {
        public Task SetGuildAnnounce(ulong guildId, bool announce);

        public Task CommandSetGuildAnnounce(IBotInteractionContext ctx, bool hear);
    }
}
