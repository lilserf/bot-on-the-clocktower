using System;
using System.Collections.Generic;

namespace Bot.Api
{
    public interface IProcessLogger
    {
        void LogException(Exception ex, string goal);

        void LogMessage(string msg);

        void LogVerbose(string msg);

        void EnableVerboseLogging();

        IReadOnlyCollection<string> Messages { get; }
    }
}
