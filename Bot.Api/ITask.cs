using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface ITask
    {
        Task Delay(TimeSpan millisecondsDelay);
    }
}
