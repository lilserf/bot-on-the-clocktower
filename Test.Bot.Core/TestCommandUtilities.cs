using Bot.Api;
using Bot.Core;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCommandUtilities : TestBase
    {
        private class TestInteractionErrorHandler : BaseInteractionErrorHandler<int>
        {
            public TestInteractionErrorHandler(IServiceProvider serviceProvider) 
                : base(serviceProvider)
            {}

            protected override string GetFriendlyStringForKey(int key) => key.ToString();
        }

        private readonly Mock<IProcessLogger> m_processLoggerMock;
        private readonly Mock<IProcessLoggerFactory> m_processLoggerFactoryMock;
        private readonly List<string> m_processLoggerMessages = new();

        public TestCommandUtilities()
        {
            m_processLoggerMock = new(MockBehavior.Strict);
            m_processLoggerFactoryMock = RegisterMock(new Mock<IProcessLoggerFactory>(MockBehavior.Strict));
            m_processLoggerFactoryMock.Setup(plf => plf.Create()).Returns(m_processLoggerMock.Object);
            m_processLoggerMock.SetupGet(pl => pl.Messages).Returns(m_processLoggerMessages);
        }

        [Fact]
        public void InteractionWrapper_NoException_InnerFuncCalled()
        {
            Mock<IMember> mockRequester = new();
            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();
            mockFunc.Setup(f => f.Invoke(It.IsAny<IProcessLogger>())).ReturnsAsync(InteractionResult.FromMessage("message"));

            TestInteractionErrorHandler ih = new(GetServiceProvider());
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(123, mockRequester.Object, mockFunc.Object));

            mockFunc.Verify(m => m(It.IsAny<IProcessLogger>()), Times.Once);
            mockRequester.Verify(a => a.SendMessageAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void InteractionWrapper_NoException_InnerFuncPassedProcessLogger()
        {
            Mock<IMember> mockAuthor = new();

            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();

            bool isProcessLoggerEqual = false;

            mockFunc.Setup(f => f.Invoke(It.IsAny<IProcessLogger>())).Callback<IProcessLogger>(pl =>
            {
                isProcessLoggerEqual = m_processLoggerMock.Object == pl;
            }).ReturnsAsync(InteractionResult.FromMessage("message"));

            TestInteractionErrorHandler ih = new(GetServiceProvider());
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(123, mockAuthor.Object, mockFunc.Object));

            Assert.True(isProcessLoggerEqual, "Process Logger from factory not passed");
            mockAuthor.Verify(a => a.SendMessageAsync(It.IsAny<string>()), Times.Never);
            m_processLoggerFactoryMock.Verify(lf => lf.Create(), Times.Once);
            mockFunc.Verify(mf => mf.Invoke(It.IsAny<IProcessLogger>()), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_UnhandledException_AuthorSentErrorMessage()
        {
            Mock<IMember> mockAuthor = new();
            int mockKey = 123;

            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            TestInteractionErrorHandler ih = new(GetServiceProvider());
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(mockKey, mockAuthor.Object, mockFunc.Object));

            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(mockKey.ToString()))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.GetType().Name))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.Message))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.StackTrace!))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(@"https://github.com/lilserf/bot-on-the-clocktower/issues"))), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_UnhandledException_ExceptionSendingToAuthor_ContinuesAnyway()
        {
            Mock<IMember> mockAuthor = new();

            mockAuthor.Setup(m => m.SendMessageAsync(It.IsAny<string>())).ThrowsAsync(new ApplicationException());

            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            TestInteractionErrorHandler ih = new(GetServiceProvider());
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(123, mockAuthor.Object, mockFunc.Object));
        }


        [Fact(Skip="Not yet implemented")]
        public void InteractionWrapper_ProcessTakesTooLong_StartsLogging()
        {
            Mock<IMember> mockAuthor = new();

            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();

            var tcs = new TaskCompletionSource<InteractionResult>();

            mockFunc.Setup(f => f.Invoke(It.IsAny<IProcessLogger>())).Returns(tcs.Task);

            TestInteractionErrorHandler ih = new(GetServiceProvider());
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(123, mockAuthor.Object, mockFunc.Object));

            // TODO: TCS should take a while (may not be able to use AssertCompletedTask), and verify that PL had a "LogVerboseMessages" method called (or something)

            mockAuthor.Verify(a => a.SendMessageAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
