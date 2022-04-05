using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IOldCommandReminder
    {
        Task UserMessageCreated(string message, IChannel channel);
    }
}
