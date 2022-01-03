using System;
using System.Collections.Generic;

namespace Bot.Api
{
    public interface IProcessLogger
    {
        public void LogException(Exception ex, string goal);

        public void LogMessage(string msg);

        public IReadOnlyCollection<string> Messages { get; }

        public bool HasMessages { get; }
    }
}
