using Bot.Core;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestProcessLogger : TestBase
    {
        private readonly Mock<ILogger> m_mockLogger = new(MockBehavior.Strict);

        public TestProcessLogger()
        {
            m_mockLogger.Setup(l => l.Debug(It.IsAny<string>(), It.IsAny<string>()));
        }

        [Fact]
        public void LogWarning_GoesToSerilog()
        {
            var pl = new ProcessLogger(m_mockLogger.Object);

            string message = "this is a message";
            pl.LogMessage(message);

            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message)), Times.Once);
            m_mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public void LogVerbose_NoSerilogCalls()
        {
            var pl = new ProcessLogger(m_mockLogger.Object);

            string message = "this is a message";
            pl.LogVerbose(message);

            m_mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public void EnableVerbose_LogVerbose_SerilogCall()
        {
            var pl = new ProcessLogger(m_mockLogger.Object);

            pl.EnableVerboseLogging();

            string message = "this is a message";
            pl.LogVerbose(message);

            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message)), Times.Once);
            m_mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public void LogVerbose_EnableVerbose_SerilogCall()
        {
            var pl = new ProcessLogger(m_mockLogger.Object);

            string message = "this is a message";
            pl.LogVerbose(message);

            pl.EnableVerboseLogging();

            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message)), Times.Once);
            m_mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public void LogVerboseMultipleTimes_EnableVerbose_SerilogCalls()
        {
            var pl = new ProcessLogger(m_mockLogger.Object);

            string message1 = "this is message1";
            string message2 = "this is message2";
            pl.LogVerbose(message1);
            pl.LogVerbose(message2);

            pl.EnableVerboseLogging();

            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message1)), Times.Once);
            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message2)), Times.Once);
            m_mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public void LogVerbose_EnableVerboseMultipleTimes_OneMessage()
        {
            var pl = new ProcessLogger(m_mockLogger.Object);

            string message = "this is a message";
            pl.LogVerbose(message);

            pl.EnableVerboseLogging();
            pl.EnableVerboseLogging();

            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message)), Times.Once);
            m_mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public void Log_Enable_Alternate_OneMessageEach()
        {
            var pl = new ProcessLogger(m_mockLogger.Object);

            string message1 = "this is a message1";
            string message2 = "this is a message2";
            string message3 = "this is a message3";

            pl.LogVerbose(message1);

            pl.EnableVerboseLogging();

            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message1)), Times.Once);
            pl.LogVerbose(message2);
            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message2)), Times.Once);

            pl.EnableVerboseLogging();
            pl.LogVerbose(message3);
            m_mockLogger.Verify(l => l.Debug(It.IsAny<string>(), It.Is<string>(m => m == message3)), Times.Once);

            m_mockLogger.VerifyNoOtherCalls();
        }
    }
}
