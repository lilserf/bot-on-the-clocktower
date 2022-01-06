using Bot.Api;
using Bot.Core;
using Bot.Core.Callbacks;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestCallbackScheduler : TestBase
    {
        private static readonly DateTime s_defaultDateTimeNow = new(2021, 2, 3, 4, 5, 6, 7);

        private readonly Mock<IDateTime> MockDateTime = new();
        private readonly Mock<ITask> MockTask = new();

        private readonly AsyncAutoResetEvent m_delayReset = new();

        private DateTime m_currentTime = s_defaultDateTimeNow;


        public TestCallbackScheduler()
        {
            RegisterMock(MockDateTime);
            RegisterMock(MockTask);

            MockDateTime.SetupGet(dt => dt.Now).Returns(() => m_currentTime);
            MockTask.Setup(t => t.Delay(It.IsAny<TimeSpan>())).Returns(async () =>
            {
                bool success = await m_delayReset.WaitOneAsync(TimeSpan.FromMilliseconds(50));
                Assert.True(success);
            });
        }

        private void AdvanceTime(TimeSpan span)
        {
            m_currentTime += span;
        }

        private DateTime GetTimeAfter(TimeSpan span)
        {
            return m_currentTime + span;
        }

        [Fact]
        public void CoreServices_RegistersCallbackSchedulerFactory()
        {
            var sp = ServiceFactory.RegisterCoreServices(null);
            var csf = sp.GetService<ICallbackSchedulerFactory>();

            Assert.IsType<CallbackSchedulerFactory>(csf);
        }

        [Fact]
        public void SchedulerFactory_CallwithCallback_ProvidesCallbackScheduler()
        {
            Mock<Func<object, Task>> mockCb = new();

            var csf = new CallbackSchedulerFactory(GetServiceProvider());
            var factory = csf.CreateScheduler(mockCb.Object, TimeSpan.FromSeconds(1));

            Assert.NotNull(factory);
        }

        [Fact]
        public async Task CallbackScheduler_ScheduleCallback_NotCalledYet()
        {
            var helper = new CallbackHelper(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);

            Assert.Equal(0, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackScheduler_ScheduleCallback_CalledAfterDelay()
        {
            var helper = new CallbackHelper(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);
            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);
            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await helper.WaitCallbackAsync();

            MockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackScheduler_CancelCallback_NeverCalled()
        {
            var helper = new CallbackHelper(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);

            helper.CancelCallback();

            MockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            MockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);

            AdvanceTime(TimeSpan.FromSeconds(10));
            m_delayReset.Set();
            await Task.Delay(5);

            Assert.Equal(0, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackScheduler_ChangeCallTime_UsesNewValue()
        {
            var helper = new CallbackHelper(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(2);
            Assert.Equal(0, helper.CallbackCount);

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(10)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(2);
            Assert.Equal(0, helper.CallbackCount);

            AdvanceTime(TimeSpan.FromSeconds(20));
            m_delayReset.Set();
            await helper.WaitCallbackAsync();

            MockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            MockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackScheduler_NoCallbacks_NeverTicked()
        {
            var helper = new CallbackHelper(GetServiceProvider());

            AdvanceTime(TimeSpan.FromSeconds(20));
            await Task.Delay(10);

            MockDateTime.VerifyGet(dt => dt.Now, Times.Never);
            MockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CallbackScheduler_FiredOnce_OnlyTickedOnce()
        {
            var helper = new CallbackHelper(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(2)));

            AdvanceTime(TimeSpan.FromSeconds(20));
            m_delayReset.Set();
            await helper.WaitCallbackAsync();

            Assert.Equal(1, helper.CallbackCount);
            MockDateTime.VerifyGet(dt => dt.Now, Times.Once);
            MockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Once);
        }

        private class CallbackHelper
        {
            public int CallbackCount { get; private set; } = 0;

            private readonly CallbackScheduler<int> m_callbackScheduler;

            private readonly AsyncAutoResetEvent m_resetEvent = new();

            private const int Key = 7;

            public CallbackHelper(IServiceProvider serviceProvider)
            {
                m_callbackScheduler = new(serviceProvider, Callback, TimeSpan.FromSeconds(1));
            }

            public void ScheduleCallback(DateTime callTime)
            {
                m_callbackScheduler.ScheduleCallback(Key, callTime);
            }

            public void CancelCallback()
            {
                m_callbackScheduler.CancelCallback(Key);
            }

            public async Task WaitCallbackAsync()
            {
                bool success = await m_resetEvent.WaitOneAsync(TimeSpan.FromMilliseconds(50));
                Assert.True(success);
            }

            private Task Callback(int key)
            {
                Assert.Equal(key, Key);
                ++CallbackCount;
                m_resetEvent.Set();
                return Task.CompletedTask;
            }
        }
    }
}
