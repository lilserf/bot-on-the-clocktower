using Bot.Api;
using Bot.Base;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;

namespace Test.Bot.Core.Interactions
{
    public static class TestInteractionErrorHandlerHelper
    {
        private static readonly InteractionResult MockReturnedErrorResult = "an error happened! oh, no!";

        /// <summary>
        /// Test that a method is properly using the Guild Error Handler
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test. We expect it calls into the Guild Error Handler</param>
        /// <param name="verifyParams">Verification action to assert that params passed to the error handler are correct</param>
        public static void TestGuildErrorHandlingRequested(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<ulong, IMember>? verifyParams)
        {
            TestErrorHandlingRequested<ulong, IGuildInteractionErrorHandler>(serviceProvider, performTest, verifyParams);
        }

        /// <summary>
        /// Test that a method is properly using the Guild Error Handler
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test. We expect it calls into the Guild Error Handler</param>
        /// <param name="verifyParams">Verification action to assert that params passed to the error handler are correct</param>
        public static void TestTownErrorHandlingRequested(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<TownKey, IMember>? verifyParams)
        {
            TestErrorHandlingRequested<TownKey, ITownInteractionErrorHandler>(serviceProvider, performTest, verifyParams);
        }

        private static void TestErrorHandlingRequested<TKey, TErrorHandlerType>(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<TKey, IMember>? verifyParams) where TKey : notnull where TErrorHandlerType : class, IInteractionErrorHandler<TKey>
        {
            Mock<TErrorHandlerType> mockErrorHandler = new();

            ServiceProvider sp = new(serviceProvider);
            sp.AddService(mockErrorHandler.Object);

            mockErrorHandler.Setup(eh => eh.TryProcessReportingErrorsAsync(It.IsAny<TKey>(), It.IsAny<IMember>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()))
                .Callback<TKey, IMember, Func<IProcessLogger, Task<InteractionResult>>>(
                (k, m, _) =>
                {
                    verifyParams?.Invoke(k, m);
                })
                .ReturnsAsync(MockReturnedErrorResult);

            TestTaskHelper.AssertCompletedTask(() => performTest(sp));

            mockErrorHandler.Verify(eh => eh.TryProcessReportingErrorsAsync(It.IsAny<TKey>(), It.IsAny<IMember>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()), Times.Once);
        }

        /// <summary>
        /// Test that a method calling into the Guild error handler behaves as expected
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test</param>
        /// <param name="verifyResult">Verification action to assert that the result of the test is correct</param>
        public static void TestGuildErrorHandlingMethod(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<Mock<IProcessLogger>, InteractionResult>? verifyResult)
        {
            TestErrorHandlingMethod<ulong, IGuildInteractionErrorHandler>(serviceProvider, performTest, verifyResult);
        }

        /// <summary>
        /// Test that a method calling into the Town error handler behaves as expected
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test</param>
        /// <param name="verifyResult">Verification action to assert that the result of the test is correct</param>
        public static void TestTownErrorHandlingMethod(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<Mock<IProcessLogger>, InteractionResult>? verifyResult)
        {
            TestErrorHandlingMethod<TownKey, ITownInteractionErrorHandler>(serviceProvider, performTest, verifyResult);
        }

        public static void TestErrorHandlingMethod<TKey, TErrorHandlerType>(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<Mock<IProcessLogger>, InteractionResult>? verifyResult) where TKey : notnull where TErrorHandlerType : class, IInteractionErrorHandler<TKey>
        {
            Mock<IGuildInteractionErrorHandler> mockErrorHandler = new();

            ServiceProvider sp = new(serviceProvider);
            sp.AddService(mockErrorHandler.Object);

            mockErrorHandler.Setup(eh => eh.TryProcessReportingErrorsAsync(It.IsAny<ulong>(), It.IsAny<IMember>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()))
                .Callback<ulong, IMember, Func<IProcessLogger, Task<InteractionResult>>>(
                    (k, m, f) =>
                    {
                        var mockPl = new Mock<IProcessLogger>(MockBehavior.Loose);

                        var result = TestTaskHelper.AssertCompletedTask(() => f(mockPl.Object));

                        verifyResult?.Invoke(mockPl, result);
                    })
                .ReturnsAsync(MockReturnedErrorResult);

            TestTaskHelper.AssertCompletedTask(() => performTest(sp));

            mockErrorHandler.Verify(eh => eh.TryProcessReportingErrorsAsync(It.IsAny<ulong>(), It.IsAny<IMember>(), It.IsAny<Func<IProcessLogger, Task<InteractionResult>>>()), Times.Once);
        }
    }
}
