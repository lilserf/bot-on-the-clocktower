using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCommandUtilities : TestBase
    {
        [Fact]
        public void InteractionWrapper_NoException_InnerFuncCalled()
        {
            Mock<IBotInteractionContext> mockContext = new();

            Mock<Func<IProcessLogger, Task>> mockFunc = new();

            var t = InteractionWrapper.TryProcessReportingErrors(mockContext.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            mockFunc.Verify(m => m(It.IsAny<IProcessLogger>()), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_NoException_InnerFuncPassedProcessLogger()
        {
            Mock<IBotInteractionContext> mockContext = new();

            Mock<Func<IProcessLogger, Task>> mockFunc = new();

            var t = InteractionWrapper.TryProcessReportingErrors(mockContext.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            mockFunc.Verify(m => m(It.IsNotNull<IProcessLogger>()), Times.Once);
            mockFunc.Verify(m => m(It.Is<IProcessLogger>(pl => pl.GetType() == typeof(ProcessLogger))), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_UnhandledException_AuthorSentErrorMessage()
        {
            Mock<IMember> mockAuthor = new();
            Mock<IBotInteractionContext> mockContext = new();

            mockContext.SetupGet(c => c.Member).Returns(mockAuthor.Object);

            Mock<Func<IProcessLogger, Task>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            var t = InteractionWrapper.TryProcessReportingErrors(mockContext.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.Message))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.StackTrace!))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(@"https://github.com/lilserf/bot-on-the-clocktower/issues"))), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_UnhandledException_ExceptionSendingToAuthor_ContinuesAnyway()
        {
            Mock<IMember> mockAuthor = new();
            Mock<IBotInteractionContext> mockContext = new();

            mockContext.SetupGet(c => c.Member).Returns(mockAuthor.Object);

            mockAuthor.Setup(m => m.SendMessageAsync(It.IsAny<string>())).ThrowsAsync(new ApplicationException());

            Mock<Func<IProcessLogger, Task>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            var t = InteractionWrapper.TryProcessReportingErrors(mockContext.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }
    }
}
