using Bot.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Main
{
    public class TaskStatic : ITask
    {
        public Task Delay(TimeSpan delay, CancellationToken ct) => Task.Delay(delay, ct);
    }
}
