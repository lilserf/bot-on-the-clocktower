﻿using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class ProcessLogger : IProcessLogger
    {
        private List<string> m_messages = new();

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
        }

		public void LogMessage(string msg)
		{
            m_messages.Add(msg);
		}

        public IReadOnlyCollection<string> Messages => m_messages;

        public bool HasMessages => m_messages.Count > 0;
	}
}
