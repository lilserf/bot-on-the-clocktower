using System.Threading.Tasks;

namespace Bot.Core
{
    public interface IApplicationShutdown
    {
        Task WhenShutdownRequested();
    }
}
