using Bot.Api;
using Serilog;
using System;
using System.Collections.Generic;

namespace Bot.Core
{
    public class ProcessLogger : IProcessLogger
    {
        private readonly ILogger m_logger;

        private bool m_verboseEnabled = false;

        private readonly List<string> m_storedVerboseMessages = new();

        public ProcessLogger(ILogger logger)
        {
            m_logger = logger;
        }

        private readonly List<string> m_messages = new();

        public void LogException(Exception ex, string goal)
		{
            string message = $"Couldn't {goal} due to unknown error!";
            if (ex is UnauthorizedException)
            {
                message = $"Couldn't {goal} due to lack of permissions.";
            }
            else if (ex is ServerErrorException) // teehee
            {
                message = $"Couldn't {goal} due to a server error.";
            }
            else if (ex is RequestSizeException) // grrr
            {
                message = $"Couldn't {goal} due to bad request size?";
            }
            else if (ex is RateLimitException) // nice watch
            {
                message = $"Couldn't {goal} due to rate limits.";
            }
            else if (ex is BadRequestException) // no more common market
            {
                message = $"Couldn't {goal} - somehow resulted in a bad request.";
            }
            else if (ex is NotFoundException) // can't think of something clever here
            {
                message = $"Couldn't {goal} - something was not found!";
            }
            m_messages.Add(message);
            m_logger.Debug("ProcessLogger caught exception {ex} while {goal}", ex, goal);
        }

		public void LogMessage(string msg)
		{
            m_messages.Add(msg);
            m_logger.Debug("ProcessLogger message: {msg}", msg);
		}

        public void LogVerbose(string msg)
        {
            if (m_verboseEnabled)
                SendVerboseMessage(msg);
            else
                m_storedVerboseMessages.Add(msg);
        }

        public void EnableVerboseLogging()
        {
            m_verboseEnabled = true;
            foreach(var msg in m_storedVerboseMessages)
                SendVerboseMessage(msg);
            m_storedVerboseMessages.Clear();
        }

        public IReadOnlyCollection<string> Messages => m_messages;

        private void SendVerboseMessage(string msg)
        {
            m_logger.Debug("ProcessLogger verbose message: {msg}", msg);
        }
	}
}
