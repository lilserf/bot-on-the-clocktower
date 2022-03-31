using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IFinalShutdownService
    {
        Task ReadyToShutdown { get; }
    }
}
