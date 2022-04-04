using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public interface IGuildInteractionErrorHandler
    {
        Task<InteractionResult> TryProcessReportingErrorsAsync(ulong guildId, IMember requester, Func<IProcessLogger, Task<InteractionResult>> process);
    }
}
