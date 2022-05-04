using Bot.Api;
using Bot.Base;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Interaction
{
    public class TestInteractionWrapperHelper
    {
        public static void TestGuildInteractionRequested(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<string, IBotInteractionContext>? verifyParams = null)
        {
            TestInteractionRequested<ulong, IGuildInteractionWrapper>(serviceProvider, performTest, verifyParams);
        }

        public static void TestTownInteractionRequested(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<string, IBotInteractionContext>? verifyParams = null)
        {
            TestInteractionRequested<TownKey, ITownInteractionWrapper>(serviceProvider, performTest, verifyParams);
        }

        public static void TestInteractionRequested<TKey, TWrapper>(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<string, IBotInteractionContext>? verifyParams)
            where TKey : notnull
            where TWrapper : class, IInteractionWrapper<TKey>
        {
            Mock<TWrapper> mockIr = new(MockBehavior.Strict);

            ServiceProvider sp = new(serviceProvider);
            sp.AddService(mockIr.Object);

            mockIr.Setup(i => i.WrapInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()))
                .Callback<string, IBotInteractionContext, Func<IProcessLogger, Task<InteractionResult>>>(
                (s, ic, f) =>
                {
                    verifyParams?.Invoke(s, ic);
                })
                .Returns(Task.CompletedTask);

            TestTaskHelper.AssertCompletedTask(() => performTest(sp));

            mockIr.Verify(iq => iq.WrapInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()), Times.Once);
        }

        public static void TestGuildInteractionMethod(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<Mock<IProcessLogger>, InteractionResult>? verifyResult)
        {
            TestInteractionMethod<ulong, IGuildInteractionWrapper>(serviceProvider, performTest, verifyResult);
        }

        public static void TestTownInteractionMethod(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<Mock<IProcessLogger>, InteractionResult>? verifyResult)
        {
            TestInteractionMethod<TownKey, ITownInteractionWrapper>(serviceProvider, performTest, verifyResult);
        }

        public static void TestInteractionMethod<TKey, TWrapper>(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<Mock<IProcessLogger>, InteractionResult>? verifyResult)
            where TKey : notnull
            where TWrapper : class, IInteractionWrapper<TKey>
        {
            Mock<TWrapper> mockIr = new(MockBehavior.Strict);

            ServiceProvider sp = new(serviceProvider);
            sp.AddService(mockIr.Object);

            InteractionResult? innerFuncResult = null;

            mockIr.Setup(i => i.WrapInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()))
                .Callback<string, IBotInteractionContext, Func<IProcessLogger, Task<InteractionResult>>>(
                    (_, _, f) =>
                    {
                        var mockPl = new Mock<IProcessLogger>(MockBehavior.Loose);

                        var result = TestTaskHelper.AssertCompletedTask(() => f(mockPl.Object));

                        verifyResult?.Invoke(mockPl, result);

                        innerFuncResult = result;
                    })
                .Returns(Task.CompletedTask);

            TestTaskHelper.AssertCompletedTask(() => performTest(sp));

            Assert.NotNull(innerFuncResult);
            mockIr.Verify(i => i.WrapInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()), Times.Once);
        }
    }
}
