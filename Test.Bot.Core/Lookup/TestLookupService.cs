using Bot.Api;
using Bot.Api.Database;
using Bot.Core;
using Bot.Core.Lookup;
using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestLookupService : TestBase, IDisposable
    {
        private readonly Mock<IGuildInteractionQueue> m_mockGuildInteractionQueue = new(MockBehavior.Strict);
        private readonly Mock<IBotInteractionContext> m_mockInteractionContext = new(MockBehavior.Strict);
        private readonly Mock<IGuildInteractionErrorHandler> m_mockGuildErrorHandler = new(MockBehavior.Strict);
        private readonly Mock<ICharacterLookup> m_mockCharacterLookup = new(MockBehavior.Strict);
        private readonly Mock<IGuild> m_mockInteractionGuild = new(MockBehavior.Strict);
        private readonly Mock<IMember> m_mockInteractionAuthor = new(MockBehavior.Strict);
        private readonly Mock<IChannel> m_mockInteractionChannel = new(MockBehavior.Strict);
        private readonly Mock<ILookupRoleDatabase> m_mockLookupDb = new(MockBehavior.Strict);
        private readonly Mock<IProcessLogger> m_mockProcessLogger = new(MockBehavior.Strict);
        private readonly Mock<ILookupMessageSender> m_mockLookupMessageSender = new(MockBehavior.Strict);

        private readonly List<string> m_mockDbScriptUrls = new();
        private ulong m_mockGuildId = 123ul;
        private string m_mockInteractionChannelName = "channel name";

        private Action<string>? m_verifyQueueString = null;

        private bool m_expectedErrorHandlerCalled = false;
        private string? m_errorHandlerResult = null;

        private Action<QueuedInteractionResult>? m_verifyInteractionResult = null;
        private QueuedInteractionResult? m_interactionResult = null;

        const string MockErrorReturnedFromErrorHandler = "an error happened! oh, no!";

        public TestLookupService()
        {
            RegisterMock(m_mockLookupDb);
            RegisterMock(m_mockGuildInteractionQueue);
            RegisterMock(m_mockGuildErrorHandler);
            RegisterMock(m_mockCharacterLookup);
            RegisterMock(m_mockLookupMessageSender);

            m_mockLookupDb.Setup(ld => ld.AddScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.RemoveScriptUrlAsync(It.IsAny<ulong>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            m_mockLookupDb.Setup(ld => ld.GetScriptUrlsAsync(It.IsAny<ulong>())).ReturnsAsync(m_mockDbScriptUrls);
            m_mockCharacterLookup.Setup(cl => cl.LookupCharacterAsync(It.IsAny<ulong>(), It.IsAny<string>())).ReturnsAsync(new LookupCharacterResult(Enumerable.Empty<LookupCharacterItem>()));
            m_mockProcessLogger.Setup(pl => pl.LogException(It.IsAny<Exception>(), It.IsAny<string>()));

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
            m_mockInteractionContext.SetupGet(ic => ic.Channel).Returns(m_mockInteractionChannel.Object);

            m_mockInteractionChannel.SetupGet(c => c.Name).Returns(m_mockInteractionChannelName);

            SetupQueueInteraction()
                .Returns<string, IBotInteractionContext, Func<Task<QueuedInteractionResult>>>((_, _, f) =>
                {
                    var task = f();
                    task.Wait(10);
                    Assert.True(task.IsCompleted);
                    Assert.Null(m_interactionResult);
                    m_interactionResult = task.Result;
                    return task; //NOTE: The real queue returns nearly immediately. This test queue will return when the actual method does.
                });
        }

        public void Dispose()
        {
            if (!m_expectedErrorHandlerCalled)
            {
                Assert.False(m_verifyInteractionResult == null && m_interactionResult != null, "The interaction was called but no verification method was set.");
                Assert.False(m_verifyInteractionResult != null && m_interactionResult == null, "An interaction verification was set but the interaction was never called.");
                if (m_verifyInteractionResult != null && m_interactionResult != null)
                    m_verifyInteractionResult(m_interactionResult);
            }
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
            PerformErrorHandlerTest(() =>
            {
                var bls = new BotLookupService(GetServiceProvider());
                AssertCompletedTask(() => bls.LookupAsync(m_mockInteractionContext.Object, "lookup string"));

                m_mockCharacterLookup.VerifyNoOtherCalls();
            });
        }

        [Fact]
        public void AddRequested_PerformsErrorHandling()
        {
            PerformErrorHandlerTest(() =>
            {
                var bls = new BotLookupService(GetServiceProvider());
                AssertCompletedTask(() => bls.AddScriptAsync(m_mockInteractionContext.Object, "add string"));

                m_mockLookupDb.VerifyNoOtherCalls();
            });
        }

        [Fact]
        public void RemoveRequested_PerformsErrorHandling()
        {
            PerformErrorHandlerTest(() =>
            {
                var bls = new BotLookupService(GetServiceProvider());
                AssertCompletedTask(() => bls.RemoveScriptAsync(m_mockInteractionContext.Object, "remove string"));

                m_mockLookupDb.VerifyNoOtherCalls();
            });
        }

        [Fact]
        public void ListScripts_PerformsErrorHandling()
        {
            PerformErrorHandlerTest(() =>
            {
                var bls = new BotLookupService(GetServiceProvider());
                AssertCompletedTask(() => bls.ListScriptsAsync(m_mockInteractionContext.Object));

                m_mockLookupDb.VerifyNoOtherCalls();
            });
        }

        private void PerformErrorHandlerTest(Action testAction)
        {
            m_expectedErrorHandlerCalled = true;
            SetupErrorHandler().Returns(Task.FromResult(MockErrorReturnedFromErrorHandler));

            testAction();

            m_mockGuildErrorHandler.Verify(teh => teh.TryProcessReportingErrorsAsync(It.IsAny<ulong>(), It.IsAny<IMember>(), It.IsAny<Func<IProcessLogger, Task<string>>>()), Times.Once);
            Assert.Equal(MockErrorReturnedFromErrorHandler, m_errorHandlerResult);
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

        [Fact]
        public void LookupNoCharacters_MessageAboutNoneFound()
        {
            string lookupStr = "lookup str";

            m_verifyInteractionResult = r =>
            {
                Assert.Contains("no ", r.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains("found", r.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains(lookupStr, r.Message);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.LookupAsync(m_mockInteractionContext.Object, lookupStr);
        }

        [Fact]
        public void LookupMultipleCharacters_CallsMessageSender()
        {
            string lookupStr = "lookup str";

            var testItem1 = new LookupCharacterItem(new CharacterData("char1", "abil1", CharacterTeam.Fabled, isOfficial: true), Enumerable.Empty<ScriptData>());
            var testItem2 = new LookupCharacterItem(new CharacterData("char2", "abil2", CharacterTeam.Townsfolk, isOfficial: false), Enumerable.Empty<ScriptData>());
            var expectedMessageCharacters = new[] { testItem1, testItem2 };

            m_mockCharacterLookup.Setup(cl => cl.LookupCharacterAsync(It.Is<ulong>(l => l == m_mockGuildId), It.Is<string>(s => s == lookupStr))).ReturnsAsync(new LookupCharacterResult(expectedMessageCharacters));

            List<LookupCharacterItem> actualMessageCharacters = new();
            m_mockLookupMessageSender
                .Setup(lms => lms.SendLookupMessageAsync(It.Is<IChannel>(c => c == m_mockInteractionChannel.Object), It.IsAny<LookupCharacterItem>()))
                .Callback<IChannel, LookupCharacterItem>((_, i) => actualMessageCharacters.Add(i))
                .Returns(Task.CompletedTask);

            m_verifyInteractionResult = r =>
            {
                Assert.Contains("found", r.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains(expectedMessageCharacters.Length.ToString(), r.Message);
                Assert.Contains(lookupStr, r.Message);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.LookupAsync(m_mockInteractionContext.Object, lookupStr);

            Assert.Equal(expectedMessageCharacters, actualMessageCharacters);
        }

        [Fact]
        public void SendMessage_ThrowsPermissionException_LoggerUpdated()
        {
            string lookupStr = "lookup str";
            var thrownException = new UnauthorizedException();
            var testItem1 = new LookupCharacterItem(new CharacterData("char1", "abil1", CharacterTeam.Fabled, isOfficial: true), Enumerable.Empty<ScriptData>());
            var testItem2 = new LookupCharacterItem(new CharacterData("char2", "abil2", CharacterTeam.Townsfolk, isOfficial: false), Enumerable.Empty<ScriptData>());
            m_mockCharacterLookup.Setup(cl => cl.LookupCharacterAsync(It.Is<ulong>(l => l == m_mockGuildId), It.IsAny<string>())).ReturnsAsync(new LookupCharacterResult(new[] { testItem1, testItem2 }));
            m_mockLookupMessageSender.Setup(lms => lms.SendLookupMessageAsync(It.IsAny<IChannel>(), It.IsAny<LookupCharacterItem>())).Throws(thrownException);

            m_verifyInteractionResult = r =>
            {
                Assert.Contains("unable", r.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.Contains(lookupStr, r.Message);
            };

            var bls = new BotLookupService(GetServiceProvider());
            bls.LookupAsync(m_mockInteractionContext.Object, lookupStr);

            m_mockProcessLogger.Verify(pl => pl.LogException(It.Is<Exception>(e => e == thrownException), It.Is<string>(s => s.Contains(m_mockInteractionChannelName))), Times.Once);
        }
    }
}
