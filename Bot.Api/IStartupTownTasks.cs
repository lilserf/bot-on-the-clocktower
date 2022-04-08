using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IStartupTownTasks
    {
        void AddStartupTask(Func<TownKey, Task> startupTask);

        Task Startup();
    }
}
