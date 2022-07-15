using Bot.Api;

namespace Bot.Core
{
    public interface IProcessLoggerFactory
    {
        IProcessLogger Create();
    }

    public class ProcessLoggerFactory : IProcessLoggerFactory
    {
        public IProcessLogger Create() => new ProcessLogger();
    }
}
