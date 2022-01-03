using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
