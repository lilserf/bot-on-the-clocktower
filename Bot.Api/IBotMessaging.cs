using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotMessaging
    {
        public Task<string> SendEvilMessage(IMember demon, IReadOnlyCollection<IMember> minions);

        public Task<string> SendLunaticMessage(IMember lunatic, IReadOnlyCollection<IMember> fakeMinions);

        public Task<string> SendLegionMessage(IReadOnlyCollection<IMember> legions);

        public Task<string> SendMagicianMessage(IMember demon, IReadOnlyCollection<IMember> minions, IMember magician);

        public Task CommandEvilMessageAsync(IBotInteractionContext ctx, IMember demon, IReadOnlyCollection<IMember> minions, IMember? magician);
        public Task CommandLunaticMessageAsync(IBotInteractionContext ctx, IMember lunatic, IReadOnlyCollection<IMember> fakeMinions);
        public Task CommandLegionMessageAsync(IBotInteractionContext ctx, IReadOnlyCollection<IMember> legions);

    }
}
