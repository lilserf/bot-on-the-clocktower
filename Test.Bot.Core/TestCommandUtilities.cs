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
            TownKey mockTownKey = new(123, 456);
            Mock<IMember> mockRequester = new();

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockTownKey, mockRequester.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            mockFunc.Verify(m => m(It.IsAny<IProcessLogger>()), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_NoException_InnerFuncPassedProcessLogger()
        {
            TownKey mockTownKey = new(123, 456);
            Mock<IMember> mockRequester = new();

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockTownKey, mockRequester.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            mockFunc.Verify(m => m(It.IsNotNull<IProcessLogger>()), Times.Once);
            mockFunc.Verify(m => m(It.Is<IProcessLogger>(pl => pl.GetType() == typeof(ProcessLogger))), Times.Once);
        }

        [Fact]
        public void InteractionWrapper_UnhandledException_AuthorSentErrorMessage()
        {
            Mock<IMember> mockAuthor = new();
            Mock<IGuild> mockGuild = new();
            Mock<IChannel> mockChannel = new();
            ulong mockGuildId = 12345;
            ulong mockChannelId = 67890;
            TownKey mockTownKey = new(mockGuildId, mockChannelId);

            mockGuild.SetupGet(g => g.Id).Returns(mockGuildId);
            mockChannel.SetupGet(g => g.Id).Returns(mockChannelId);

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockTownKey, mockAuthor.Object, mockFunc.Object);
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
            TownKey mockTownKey = new(123, 456);

            mockAuthor.Setup(m => m.SendMessageAsync(It.IsAny<string>())).ThrowsAsync(new ApplicationException());

            Mock<Func<IProcessLogger, Task<string>>> mockFunc = new();

            var thrownException = new ApplicationException();
            mockFunc.Setup(m => m(It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);

            var t = InteractionWrapper.TryProcessReportingErrorsAsync(mockTownKey, mockAuthor.Object, mockFunc.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }
    }
}
