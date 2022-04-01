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
        private readonly Mock<IGuild> m_mockGuild = new(MockBehavior.Strict);
        private readonly Mock<ILookupRoleDatabase> m_mockLookupDb = new(MockBehavior.Strict);

        private readonly List<string> m_mockDbScriptUrls = new();
        private ulong m_mockGuildId = 123ul;

        private Action<QueuedInteractionResult>? m_verifyResult = null;

        public TestLookupService()
        {
            RegisterMock(m_mockLookupDb);
            RegisterMock(m_mockGuildInteractionQueue);

            m_mockLookupDb.Setup(ld => ld.AddScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.RemoveScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.GetScriptUrlsAsync(It.IsAny<ulong>())).ReturnsAsync(m_mockDbScriptUrls);

            m_mockGuild.SetupGet(g => g.Id).Returns(m_mockGuildId);

            m_mockInteractionContext.SetupGet(ic => ic.Guild).Returns(m_mockGuild.Object);

            SetupQueueInteraction()
                .Returns<string, IBotInteractionContext, Func<Task<QueuedInteractionResult>>>((_, _, f) =>
                {
                    var task = f();
                    task.Wait(10);
                    Assert.True(task.IsCompleted);
                    m_verifyResult?.Invoke(task.Result);
                    return task; //NOTE: The real queue returns nearly immediately. This test queue will return when the actual method does.
                });
        }

        [Fact]
        public void LookupRequested_QueuesRequest()
        {
            string lookupStr = "test lookup";

            var bls = SetupLookupServiceWithDoNothingQueue();
            AssertCompletedTask(() => bls.LookupAsync(m_mockInteractionContext.Object, lookupStr));

            m_mockGuildInteractionQueue.Verify(giq => giq.QueueInteractionAsync(It.Is<string>(s => s.Contains(lookupStr)), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<QueuedInteractionResult>>>()), Times.Once);
        }

        [Fact]
        public void AddScriptRequested_QueuesRequest()
        {
            string scriptUrl = "test script url";

            var bls = SetupLookupServiceWithDoNothingQueue();
            AssertCompletedTask(() => bls.AddScriptAsync(m_mockInteractionContext.Object, scriptUrl));

            m_mockGuildInteractionQueue.Verify(giq => giq.QueueInteractionAsync(It.Is<string>(s => s.Contains("adding", StringComparison.InvariantCultureIgnoreCase) && s.Contains(scriptUrl)), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<QueuedInteractionResult>>>()), Times.Once);
        }

        [Fact]
        public void RemoveScriptRequested_QueuesRequest()
        {
            string scriptUrl = "test script url";

            var bls = SetupLookupServiceWithDoNothingQueue();
            AssertCompletedTask(() => bls.RemoveScriptAsync(m_mockInteractionContext.Object, scriptUrl));

            m_mockGuildInteractionQueue.Verify(giq => giq.QueueInteractionAsync(It.Is<string>(s => s.Contains("removing", StringComparison.InvariantCultureIgnoreCase) && s.Contains(scriptUrl)), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<QueuedInteractionResult>>>()), Times.Once);
        }

        [Fact]
        public void ListScriptsRequested_QueuesRequest()
        {
            var bls = SetupLookupServiceWithDoNothingQueue();
            AssertCompletedTask(() => bls.ListScriptsAsync(m_mockInteractionContext.Object));

            m_mockGuildInteractionQueue.Verify(giq => giq.QueueInteractionAsync(It.Is<string>(s => s.Contains("scripts", StringComparison.InvariantCultureIgnoreCase)), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<QueuedInteractionResult>>>()), Times.Once);
        }

        private BotLookupService SetupLookupServiceWithDoNothingQueue()
        {
            SetupQueueInteraction().Returns(Task.CompletedTask);
            return new BotLookupService(GetServiceProvider());
        }

        private ISetup<IGuildInteractionQueue, Task> SetupQueueInteraction()
        {
            return m_mockGuildInteractionQueue
                .Setup(giq => giq.QueueInteractionAsync(It.IsAny<string>(), It.Is<IBotInteractionContext>(ic => ic == m_mockInteractionContext.Object), It.IsAny<Func<Task<QueuedInteractionResult>>>()));
        }

        [Fact]
        public void AddScriptRequested_AddsScriptToDbAndReturnsResults()
        {
            string scriptUrl = "test script url";

            m_verifyResult = r =>
            {
                Assert.Contains(scriptUrl, r.Message);
                Assert.Contains("added", r.Message);
                Assert.False(r.IncludeComponents);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.AddScriptAsync(m_mockInteractionContext.Object, scriptUrl);

            m_mockLookupDb.Verify(ld => ld.AddScriptUrlAsync(It.Is<ulong>(l => l == m_mockGuildId), It.Is<string>(s => s == scriptUrl)), Times.Once);
        }
    }
}
