using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestMembers : TestBase
    {
        private readonly Mock<IChannel> ChannelMock = new(MockBehavior.Strict);
        private readonly Mock<IMember> MemberMock = new(MockBehavior.Strict);
        private readonly Mock<IRole> RoleMock = new(MockBehavior.Strict);
        private readonly Mock<IProcessLogger> ProcessLoggerMock = new(MockBehavior.Strict);

        private const string MockChannelName = "Mock Channel";
        private const string MockRoleName = "Mock Role";
        private const string MockMemberName = "Mock Member";

        public TestMembers()
        {
            ChannelMock.SetupGet(c => c.Name).Returns(MockChannelName);
            MemberMock.SetupGet(m => m.DisplayName).Returns(MockMemberName);
            RoleMock.SetupGet(r => r.Name).Returns(MockRoleName);
            ProcessLoggerMock.Setup(pl => pl.LogException(It.IsAny<Exception>(), It.IsAny<string>()));
        }

        [Fact]
        public void MoveToChannel_NoException_NoLoggerCalls()
        {
            MemberMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).Returns(Task.CompletedTask);

            var t = MemberHelper.MoveToChannelLoggingErrorsAsync(MemberMock.Object, ChannelMock.Object, ProcessLoggerMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);
            Assert.True(t.Result);

            ProcessLoggerMock.Verify(pl => pl.LogException(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        [InlineData(typeof(ServerErrorException))]
        public void MoveToChannel_ThrowsException_LoggerUpdated(Type exceptionType)
        {
            var thrownException = CreateException(exceptionType);
            MemberMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).ThrowsAsync(thrownException);

            var t = MemberHelper.MoveToChannelLoggingErrorsAsync(MemberMock.Object, ChannelMock.Object, ProcessLoggerMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);

            ProcessLoggerMock.Verify(pl => pl.LogException(It.Is<Exception>(e => e == thrownException), It.Is<string>(s => s.Contains(MockMemberName) && s.Contains(MockChannelName))), Times.Once);
        }

        [Fact]
        public void GrantRole_NoException_NoLoggerCalls()
        {
            MemberMock.Setup(m => m.GrantRoleAsync(It.IsAny<IRole>())).Returns(Task.CompletedTask);

            var t = MemberHelper.GrantRoleLoggingErrorsAsync(MemberMock.Object, RoleMock.Object, ProcessLoggerMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);
            Assert.True(t.Result);

            ProcessLoggerMock.Verify(pl => pl.LogException(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        [InlineData(typeof(ServerErrorException))]
        public void GrantRole_ThrowsException_LoggerUpdated(Type exceptionType)
        {
            var thrownException = CreateException(exceptionType);
            MemberMock.Setup(m => m.GrantRoleAsync(It.IsAny<IRole>())).ThrowsAsync(thrownException);

            var t = MemberHelper.GrantRoleLoggingErrorsAsync(MemberMock.Object, RoleMock.Object, ProcessLoggerMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);

            ProcessLoggerMock.Verify(pl => pl.LogException(It.Is<Exception>(e => e == thrownException), It.Is<string>(s => s.Contains(MockMemberName) && s.Contains(MockRoleName))), Times.Once);
        }

        [Fact]
        public void RevokeRole_NoException_NoLoggerCalls()
        {
            MemberMock.Setup(m => m.RevokeRoleAsync(It.IsAny<IRole>())).Returns(Task.CompletedTask);

            var t = MemberHelper.RevokeRoleLoggingErrorsAsync(MemberMock.Object, RoleMock.Object, ProcessLoggerMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);
            Assert.True(t.Result);

            ProcessLoggerMock.Verify(pl => pl.LogException(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(typeof(UnauthorizedException))]
        [InlineData(typeof(NotFoundException))]
        [InlineData(typeof(ServerErrorException))]
        public void RevokeRole_ThrowsException_LoggerUpdated(Type exceptionType)
        {
            var thrownException = CreateException(exceptionType);
            MemberMock.Setup(m => m.RevokeRoleAsync(It.IsAny<IRole>())).ThrowsAsync(thrownException);

            var t = MemberHelper.RevokeRoleLoggingErrorsAsync(MemberMock.Object, RoleMock.Object, ProcessLoggerMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);

            ProcessLoggerMock.Verify(pl => pl.LogException(It.Is<Exception>(e => e == thrownException), It.Is<string>(s => s.Contains(MockMemberName) && s.Contains(MockRoleName))), Times.Once);
        }

        [Fact]
        public void MoveToChannel_UnhandledExceptopn_Throws()
        {
            MemberMock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).ThrowsAsync(new ApplicationException());

            var t = Assert.ThrowsAsync<ApplicationException>(() => MemberHelper.MoveToChannelLoggingErrorsAsync(MemberMock.Object, ChannelMock.Object, ProcessLoggerMock.Object));

            t.Wait(5);
            Assert.True(t.IsCompleted);
        }

        [Fact]
        public void GrantRole_UnhandledExceptopn_Throws()
        {
            MemberMock.Setup(m => m.GrantRoleAsync(It.IsAny<IRole>())).ThrowsAsync(new ApplicationException());

            var t = Assert.ThrowsAsync<ApplicationException>(() => MemberHelper.GrantRoleLoggingErrorsAsync(MemberMock.Object, RoleMock.Object, ProcessLoggerMock.Object));

            t.Wait(5);
            Assert.True(t.IsCompleted);
        }

        [Fact]
        public void RevokeRole_UnhandledExceptopn_Throws()
        {
            MemberMock.Setup(m => m.RevokeRoleAsync(It.IsAny<IRole>())).ThrowsAsync(new ApplicationException());

            var t = Assert.ThrowsAsync<ApplicationException>(() => MemberHelper.RevokeRoleLoggingErrorsAsync(MemberMock.Object, RoleMock.Object, ProcessLoggerMock.Object));

            t.Wait(5);
            Assert.True(t.IsCompleted);
        }
    }
}
