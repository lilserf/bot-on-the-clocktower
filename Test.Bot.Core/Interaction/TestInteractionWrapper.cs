using Bot.Api;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Interaction
{
    public class TestInteractionWrapper : TestBase
    {
        private const int MockKey = 12345;
        private const string MockInitialMessage = "Initial interaction message";

        private readonly Mock<IBotInteractionContext> m_mockInteractionContext = new();
        private readonly Mock<IMember> m_mockAuthor = new();

        public TestInteractionWrapper()
        {
            m_mockInteractionContext.SetupGet(ic => ic.Member).Returns(m_mockAuthor.Object);
        }

        [Fact]
        public void InteractionWrapped_UsesQueue()
        {
            TestInteractionQueueHelper.TestQueueRequested<ITestInteractionQueue>(GetServiceProvider(),
                (sp) =>
                {
                    TestInteractionWrapperClass iwc = new(sp);
                    return iwc.WrapInteractionAsync(MockInitialMessage, m_mockInteractionContext.Object,
                        (pl) =>
                        {
                            throw new InvalidOperationException();
                        });
                },
                (m, ic) =>
                {
                    Assert.Equal(MockInitialMessage, m);
                    Assert.Equal(m_mockInteractionContext.Object, ic);
                });
        }

        [Fact]
        public void InteractionWrapped_HandlesErrors()
        {
            TestInteractionQueueHelper.TestQueueMethod<ITestInteractionQueue>(GetServiceProvider(),
                (queueSp) =>
                {
                    TestInteractionErrorHandlerHelper.TestErrorHandlingRequested<int, ITestInteractionErrorHandler>(queueSp,
                        (sp) =>
                        {
                            TestInteractionWrapperClass iwc = new(sp);
                            return iwc.WrapInteractionAsync(MockInitialMessage, m_mockInteractionContext.Object,
                                (pl) =>
                                {
                                    throw new InvalidOperationException();
                                });
                        },
                        (k, m) =>
                        {
                            Assert.Equal(MockKey, k);
                            Assert.Equal(m_mockAuthor.Object, m);
                        });
                    return Task.CompletedTask;
                },
                (ir) =>
                {
                    Assert.Equal(TestInteractionErrorHandlerHelper.MockReturnedErrorResult, ir);
                });
        }

        [Fact]
        public void InteractionWrapped_CallsFunc()
        {
            var returnedFromFunc = InteractionResult.FromMessage("some message!");

            TestInteractionQueueHelper.TestQueueMethod<ITestInteractionQueue>(GetServiceProvider(),
                (queueSp) =>
                {
                    TestInteractionErrorHandlerHelper.TestErrorHandlingMethod<int, ITestInteractionErrorHandler>(queueSp,
                        (sp) =>
                        {
                            TestInteractionWrapperClass iwc = new(sp);
                            return iwc.WrapInteractionAsync(MockInitialMessage, m_mockInteractionContext.Object,
                                (pl) => Task.FromResult(returnedFromFunc));
                        },
                        (plm, ir) =>
                        {
                            Assert.Equal(returnedFromFunc, ir);
                        });
                    return Task.CompletedTask;
                },
                (ir) =>
                {
                    Assert.Equal(returnedFromFunc, ir);
                });
        }

        public interface ITestInteractionQueue : IInteractionQueue
        {}

        public interface ITestInteractionErrorHandler : IInteractionErrorHandler<int>
        {}

        private class TestInteractionWrapperClass : BaseInteractionWrapper<int, ITestInteractionQueue, ITestInteractionErrorHandler>
        {
            public TestInteractionWrapperClass(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {}

            protected override int KeyFromContext(IBotInteractionContext context) => MockKey;
        }
    }
}
