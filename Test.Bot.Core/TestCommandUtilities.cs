using Bot.Api;
using Bot.Core;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCommandUtilities : TestBase
    {
        private class TestInteractionErrorHandler : BaseInteractionErrorHandler<int>
        {
            protected override string GetFriendlyStringForKey(int key) => key.ToString();
        }

        [Fact]
        public void InteractionWrapper_NoException_InnerFuncCalled()
        {
            Mock<IMember> mockRequester = new();
            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();
            mockFunc.Setup(f => f.Invoke(It.IsAny<IProcessLogger>())).ReturnsAsync(InteractionResult.FromMessage("message"));

            TestInteractionErrorHandler ih = new();
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(123, mockRequester.Object, mockFunc.Object));

            mockFunc.Verify(m => m(It.IsAny<IProcessLogger>()), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_NoException_InnerFuncPassedProcessLogger()
        {
            Mock<IMember> mockRequester = new();

            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();
            mockFunc.Setup(f => f.Invoke(It.IsAny<IProcessLogger>())).ReturnsAsync(InteractionResult.FromMessage("message"));

            TestInteractionErrorHandler ih = new();
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(123, mockRequester.Object, mockFunc.Object));

            mockFunc.Verify(m => m(It.IsNotNull<IProcessLogger>()), Times.Once);
            mockFunc.Verify(m => m(It.Is<IProcessLogger>(pl => pl.GetType() == typeof(ProcessLogger))), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_UnhandledException_AuthorSentErrorMessage()
        {
            Mock<IMember> mockAuthor = new();
            int mockKey = 123;

            Mock<Func<IProcessLogger, Task<InteractionResult>>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            TestInteractionErrorHandler ih = new();
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

            TestInteractionErrorHandler ih = new();
            AssertCompletedTask(() => ih.TryProcessReportingErrorsAsync(123, mockAuthor.Object, mockFunc.Object));
        }
    }
}
