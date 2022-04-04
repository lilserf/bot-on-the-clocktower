using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public interface ITownInteractionErrorHandler
    {
        Task<InteractionResult> TryProcessReportingErrorsAsync(TownKey townKey, IMember requester, Func<IProcessLogger, Task<InteractionResult>> process);
    }
}
