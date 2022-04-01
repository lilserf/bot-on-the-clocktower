using Bot.Api;
using Bot.Api.Database;
using Bot.Core;
using Bot.Core.Lookup;
using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestLookupService : TestBase
    {
        private readonly Mock<IGuildInteractionQueue> m_mockGuildInteractionQueue = new(MockBehavior.Strict);
        private readonly Mock<IBotInteractionContext> m_mockInteractionContext = new(MockBehavior.Strict);
        private readonly Mock<IGuildInteractionErrorHandler> m_mockGuildErrorHandler = new(MockBehavior.Strict);
        private readonly Mock<ICharacterLookup> m_mockCharacterLookup = new(MockBehavior.Strict);
        private readonly Mock<IGuild> m_mockInteractionGuild = new(MockBehavior.Strict);
        private readonly Mock<IMember> m_mockInteractionAuthor = new(MockBehavior.Strict);
        private readonly Mock<ILookupRoleDatabase> m_mockLookupDb = new(MockBehavior.Strict);
        private readonly Mock<IProcessLogger> m_mockProcessLogger = new(MockBehavior.Strict);

        private readonly List<string> m_mockDbScriptUrls = new();
        private ulong m_mockGuildId = 123ul;

        private Action<string>? m_verifyQueueString = null;
        private Action<QueuedInteractionResult>? m_verifyInteractionResult = null;
        private string? m_errorHandlerResult = null;

        const string MockErrorReturnedFromErrorHandler = "an error happened! oh, no!";

        public TestLookupService()
        {
            RegisterMock(m_mockLookupDb);
            RegisterMock(m_mockGuildInteractionQueue);
            RegisterMock(m_mockGuildErrorHandler);
            RegisterMock(m_mockCharacterLookup);

            m_mockLookupDb.Setup(ld => ld.AddScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.RemoveScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.GetScriptUrlsAsync(It.IsAny<ulong>())).ReturnsAsync(m_mockDbScriptUrls);

            m_mockInteractionGuild.SetupGet(g => g.Id).Returns(m_mockGuildId);
            SetupErrorHandler()
                .Returns<ulong, IMember, Func<IProcessLogger, Task<string>>>((gid, member, f) =>
                {
                    var task = f(m_mockProcessLogger.Object);
                    task.Wait(10);
                    Assert.True(task.IsCompleted);
                    m_errorHandlerResult = task.Result;
                    return Task.FromResult(m_errorHandlerResult);
                });

            m_mockInteractionContext.SetupGet(ic => ic.Guild).Returns(m_mockInteractionGuild.Object);
            m_mockInteractionContext.SetupGet(ic => ic.Member).Returns(m_mockInteractionAuthor.Object);

            SetupQueueInteraction()
                .Returns<string, IBotInteractionContext, Func<Task<QueuedInteractionResult>>>((_, _, f) =>
                {
                    var task = f();
                    task.Wait(10);
                    Assert.True(task.IsCompleted);
                    m_verifyInteractionResult?.Invoke(task.Result);
                    return task; //NOTE: The real queue returns nearly immediately. This test queue will return when the actual method does.
                });
        }

        #region Queue tests
        [Fact]
        public void LookupRequested_QueuesRequest()
        {
            string lookupStr = "test lookup";

            m_verifyQueueString = s =>
            {
                Assert.Contains(lookupStr, s);
            };

            var bls = SetupLookupServiceWithDoNothingTask();
            AssertCompletedTask(() => bls.LookupAsync(m_mockInteractionContext.Object, lookupStr));
        }

        [Fact]
        public void AddScriptRequested_QueuesRequest()
        {
            string scriptUrl = "test script url";

            m_verifyQueueString = s =>
            {
                Assert.Contains("adding", s, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains(scriptUrl, s);
            };

            var bls = SetupLookupServiceWithDoNothingTask();
            AssertCompletedTask(() => bls.AddScriptAsync(m_mockInteractionContext.Object, scriptUrl));
        }

        [Fact]
        public void RemoveScriptRequested_QueuesRequest()
        {
            string scriptUrl = "test script url";

            m_verifyQueueString = s =>
            {
                Assert.Contains("removing", s, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains(scriptUrl, s);
            };

            var bls = SetupLookupServiceWithDoNothingTask();
            AssertCompletedTask(() => bls.RemoveScriptAsync(m_mockInteractionContext.Object, scriptUrl));
        }

        [Fact]
        public void ListScriptsRequested_QueuesRequest()
        {
            m_verifyQueueString = s =>
            {
                Assert.Contains("scripts", s, StringComparison.InvariantCultureIgnoreCase);
            };

            var bls = SetupLookupServiceWithDoNothingTask();
            AssertCompletedTask(() => bls.ListScriptsAsync(m_mockInteractionContext.Object));
        }

        private BotLookupService SetupLookupServiceWithDoNothingTask()
        {
            SetupQueueInteraction().Returns(Task.CompletedTask);
            return new BotLookupService(GetServiceProvider());
        }

        private IReturnsThrows<IGuildInteractionQueue, Task> SetupQueueInteraction()
        {
            return m_mockGuildInteractionQueue
                .Setup(giq => giq.QueueInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<QueuedInteractionResult>>>()))
                .Callback<string, IBotInteractionContext, Func<Task<QueuedInteractionResult>>>((s, ic, f) =>
                {
                    m_verifyQueueString?.Invoke(s);
                    Assert.Equal(ic, m_mockInteractionContext.Object);
                });
        }
        #endregion

        #region Error handling tests
        [Fact]
        public void LookupRequested_PerformsErrorHandling()
        {
            var bls = SetupErrorHandlerWithDoNothingTask();
            AssertCompletedTask(() => bls.LookupAsync(m_mockInteractionContext.Object, "lookup string"));

            m_mockCharacterLookup.VerifyNoOtherCalls();
            VerifyErrorHandledProperly();
        }

        [Fact]
        public void AddRequested_PerformsErrorHandling()
        {
            var bls = SetupErrorHandlerWithDoNothingTask();
            AssertCompletedTask(() => bls.AddScriptAsync(m_mockInteractionContext.Object, "add string"));

            m_mockLookupDb.VerifyNoOtherCalls();
            VerifyErrorHandledProperly();
        }

        [Fact]
        public void RemoveRequested_PerformsErrorHandling()
        {
            var bls = SetupErrorHandlerWithDoNothingTask();
            AssertCompletedTask(() => bls.RemoveScriptAsync(m_mockInteractionContext.Object, "remove string"));

            m_mockLookupDb.VerifyNoOtherCalls();
            VerifyErrorHandledProperly();
        }

        [Fact]
        public void ListScripts_PerformsErrorHandling()
        {
            var bls = SetupErrorHandlerWithDoNothingTask();
            AssertCompletedTask(() => bls.ListScriptsAsync(m_mockInteractionContext.Object));

            m_mockLookupDb.VerifyNoOtherCalls();
            VerifyErrorHandledProperly();
        }

        private IReturnsThrows<IGuildInteractionErrorHandler, Task<string>> SetupErrorHandler()
        {
            return m_mockGuildErrorHandler.Setup(teh => teh.TryProcessReportingErrorsAsync(It.IsAny<ulong>(), It.IsAny<IMember>(), It.IsAny<Func<IProcessLogger, Task<string>>>()))
                .Callback<ulong, IMember, Func<IProcessLogger, Task<string>>>((gid, member, f) =>
                {
                    m_errorHandlerResult = MockErrorReturnedFromErrorHandler;
                    Assert.Equal(m_mockGuildId, gid);
                    Assert.Equal(m_mockInteractionAuthor.Object, member);
                });
        }

        private BotLookupService SetupErrorHandlerWithDoNothingTask()
        {
            SetupErrorHandler().Returns(Task.FromResult(MockErrorReturnedFromErrorHandler));
            return new BotLookupService(GetServiceProvider());
        }

        private void VerifyErrorHandledProperly()
        {
            m_mockGuildErrorHandler.Verify(teh => teh.TryProcessReportingErrorsAsync(It.IsAny<ulong>(), It.IsAny<IMember>(), It.IsAny<Func<IProcessLogger, Task<string>>>()), Times.Once);
            Assert.Equal(MockErrorReturnedFromErrorHandler, m_errorHandlerResult);
        }
        #endregion

        [Fact]
        public void AddScriptRequested_AddsScriptToDbAndReturnsResults()
        {
            string scriptUrl = "test script url";

            m_verifyInteractionResult = r =>
            {
                Assert.Contains(scriptUrl, r.Message);
                Assert.Contains("added", r.Message);
                Assert.False(r.IncludeComponents);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.AddScriptAsync(m_mockInteractionContext.Object, scriptUrl);

            m_mockLookupDb.Verify(ld => ld.AddScriptUrlAsync(It.Is<ulong>(l => l == m_mockGuildId), It.Is<string>(s => s == scriptUrl)), Times.Once);
        }

        [Fact]
        public void RemoveScriptRequested_RemovesScriptFromDbAndReturnsResults()
        {
            string scriptUrl = "test script url";

            m_verifyInteractionResult = r =>
            {
                Assert.Contains(scriptUrl, r.Message);
                Assert.Contains("removed", r.Message);
                Assert.False(r.IncludeComponents);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.RemoveScriptAsync(m_mockInteractionContext.Object, scriptUrl);

            m_mockLookupDb.Verify(ld => ld.RemoveScriptUrlAsync(It.Is<ulong>(l => l == m_mockGuildId), It.Is<string>(s => s == scriptUrl)), Times.Once);
        }

        [Fact]
        public void NoScripts_ListScriptsRequested_ReturnsNoScripts()
        {
            m_verifyInteractionResult = r =>
            {
                Assert.Contains("no custom scripts", r.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.False(r.IncludeComponents);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.ListScriptsAsync(m_mockInteractionContext.Object);
        }

        [Fact]
        public void SomeScripts_ListScriptsRequested_ReturnsScripts()
        {
            string script1 = "script 1 url";
            string script2 = "script 2 url";
            m_mockDbScriptUrls.Add(script1);
            m_mockDbScriptUrls.Add(script2);

            m_verifyInteractionResult = r =>
            {
                Assert.Contains("custom scripts", r.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains("found", r.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains($"⦁ {script1}", r.Message);
                Assert.Contains($"⦁ {script2}", r.Message);
                Assert.False(r.IncludeComponents);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.ListScriptsAsync(m_mockInteractionContext.Object);
        }
    }
}
