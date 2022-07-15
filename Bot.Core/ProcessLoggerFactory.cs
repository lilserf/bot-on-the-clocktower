using Bot.Api;
using Serilog;

namespace Bot.Core
{
    public interface IProcessLoggerFactory
    {
        IProcessLogger Create();
    }

    public class ProcessLoggerFactory : IProcessLoggerFactory
    {
        private readonly ILogger m_logger;

        public ProcessLoggerFactory(ILogger logger)
        {
            m_logger = logger;
        }

        public IProcessLogger Create() => new ProcessLogger(m_logger);
    }
}
