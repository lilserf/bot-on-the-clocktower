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

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockContext.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            mockFunc.Verify(m => m(It.IsAny<IProcessLogger>()), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_NoException_InnerFuncPassedProcessLogger()
        {
            Mock<IBotInteractionContext> mockContext = new();

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockContext.Object, mockFunc.Object);
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
            Mock<IGuild> mockGuild = new();
            Mock<IChannel> mockChannel = new();
            ulong mockGuildId = 12345;
            ulong mockChannelId = 67890;

            mockGuild.SetupGet(g => g.Id).Returns(mockGuildId);
            mockChannel.SetupGet(g => g.Id).Returns(mockChannelId);

            mockContext.SetupGet(c => c.Member).Returns(mockAuthor.Object);
            mockContext.SetupGet(c => c.Guild).Returns(mockGuild.Object);
            mockContext.SetupGet(c => c.Channel).Returns(mockChannel.Object);

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockContext.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(mockGuildId.ToString()))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(mockChannelId.ToString()))), Times.Once);
            mockAuthor.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.GetType().Name))), Times.Once);
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

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockContext.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }
    }
}
