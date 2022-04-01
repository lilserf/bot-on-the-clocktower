using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public interface ITownInteractionErrorHandler
    {
        Task<string> TryProcessReportingErrorsAsync(TownKey townKey, IMember requester, Func<IProcessLogger, Task<string>> process);
    }
}
