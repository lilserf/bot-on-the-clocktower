using Bot.Api;
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

        private readonly Mock<IDateTime> m_mockDateTime = new();
        private readonly Mock<ITask> m_mockTask = new();

        private readonly AsyncAutoResetEvent m_delayReset = new();

        private DateTime m_currentTime = s_defaultDateTimeNow;


        public TestCallbackScheduler()
        {
            RegisterMock(m_mockDateTime);
            RegisterMock(m_mockTask);

            m_mockDateTime.SetupGet(dt => dt.Now).Returns(() => m_currentTime);
            m_mockTask.Setup(t => t.Delay(It.IsAny<TimeSpan>())).Returns(async () =>
            {
                bool success = await m_delayReset.WaitOneAsync(TimeSpan.FromSeconds(2));
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
        public void SchedulerFactoryKey_CallwithCallback_ProvidesCallbackScheduler()
        {
            Mock<Func<object, Task>> mockCb = new();

            var csf = new CallbackSchedulerFactory(GetServiceProvider());
            var factory = csf.CreateScheduler(mockCb.Object, TimeSpan.FromSeconds(1));

            Assert.NotNull(factory);
        }

        [Fact]
        public async Task CallbackSchedulerKey_ScheduleCallback_NotCalledYet()
        {
            var helper = new CallbackHelperWithKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);

            Assert.Equal(0, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerKey_ScheduleCallback_CalledAfterDelay()
        {
            var helper = new CallbackHelperWithKey(GetServiceProvider());

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

            m_mockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerKey_CancelCallback_NeverCalled()
        {
            var helper = new CallbackHelperWithKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);

            helper.CancelCallback();

            m_mockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            m_mockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);

            AdvanceTime(TimeSpan.FromSeconds(10));
            m_delayReset.Set();
            await Task.Delay(5);

            Assert.Equal(0, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerKey_ChangeCallTime_UsesNewValue()
        {
            var helper = new CallbackHelperWithKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);
            Assert.Equal(0, helper.CallbackCount);

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(10)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);
            Assert.Equal(0, helper.CallbackCount);

            AdvanceTime(TimeSpan.FromSeconds(20));
            m_delayReset.Set();
            await helper.WaitCallbackAsync();

            m_mockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            m_mockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerKey_NoCallbacks_NeverTicked()
        {
            var helper = new CallbackHelperWithKey(GetServiceProvider());

            AdvanceTime(TimeSpan.FromSeconds(20));
            await Task.Delay(10);

            m_mockDateTime.VerifyGet(dt => dt.Now, Times.Never);
            m_mockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CallbackSchedulerKey_FiredOnce_OnlyTickedOnce()
        {
            var helper = new CallbackHelperWithKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(2)));

            AdvanceTime(TimeSpan.FromSeconds(20));
            m_delayReset.Set();
            await helper.WaitCallbackAsync();

            Assert.Equal(1, helper.CallbackCount);
            m_mockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Once, "Task not called once");
        }

        [Fact]
        public void SchedulerFactoryNoKey_CallwithCallback_ProvidesCallbackScheduler()
        {
            Mock<Func<Task>> mockCb = new();

            var csf = new CallbackSchedulerFactory(GetServiceProvider());
            var factory = csf.CreateScheduler(mockCb.Object, TimeSpan.FromSeconds(1));

            Assert.NotNull(factory);
        }

        [Fact]
        public async Task CallbackSchedulerNoKey_ScheduleCallback_NotCalledYet()
        {
            var helper = new CallbackHelperNoKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);

            Assert.Equal(0, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerNoKey_ScheduleCallback_CalledAfterDelay()
        {
            var helper = new CallbackHelperNoKey(GetServiceProvider());

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

            m_mockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerNoKey_CancelCallback_NeverCalled()
        {
            var helper = new CallbackHelperNoKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);

            helper.CancelCallback();

            m_mockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            m_mockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);

            AdvanceTime(TimeSpan.FromSeconds(10));
            m_delayReset.Set();
            await Task.Delay(5);

            Assert.Equal(0, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerNoKey_ChangeCallTime_UsesNewValue()
        {
            var helper = new CallbackHelperNoKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);
            Assert.Equal(0, helper.CallbackCount);

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(10)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            m_delayReset.Set();
            await Task.Delay(5);
            Assert.Equal(0, helper.CallbackCount);

            AdvanceTime(TimeSpan.FromSeconds(20));
            m_delayReset.Set();
            await helper.WaitCallbackAsync();

            m_mockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            m_mockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1, helper.CallbackCount);
        }

        [Fact]
        public async Task CallbackSchedulerNoKey_NoCallbacks_NeverTicked()
        {
            var helper = new CallbackHelperNoKey(GetServiceProvider());

            AdvanceTime(TimeSpan.FromSeconds(20));
            await Task.Delay(10);

            m_mockDateTime.VerifyGet(dt => dt.Now, Times.Never);
            m_mockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CallbackSchedulerNoKey_FiredOnce_OnlyTickedOnce()
        {
            var helper = new CallbackHelperNoKey(GetServiceProvider());

            helper.ScheduleCallback(GetTimeAfter(TimeSpan.FromSeconds(2)));

            AdvanceTime(TimeSpan.FromSeconds(20));
            m_delayReset.Set();
            await helper.WaitCallbackAsync();

            Assert.Equal(1, helper.CallbackCount);
            m_mockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Once, "Task not called once");
        }

        private class CallbackHelperWithKey
        {
            public int CallbackCount { get; private set; } = 0;

            private readonly CallbackScheduler<int> m_callbackScheduler;

            private readonly AsyncAutoResetEvent m_resetEvent = new();

            private const int Key = 7;

            public CallbackHelperWithKey(IServiceProvider serviceProvider)
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
                bool success = await m_resetEvent.WaitOneAsync(TimeSpan.FromSeconds(2));
                Assert.True(success, "Reset event not called within wait period");
            }

            private Task Callback(int key)
            {
                Assert.Equal(key, Key);
                ++CallbackCount;
                m_resetEvent.Set();
                return Task.CompletedTask;
            }
        }

        private class CallbackHelperNoKey
        {
            public int CallbackCount { get; private set; } = 0;

            private readonly CallbackScheduler m_callbackScheduler;

            private readonly AsyncAutoResetEvent m_resetEvent = new();

            public CallbackHelperNoKey(IServiceProvider serviceProvider)
            {
                m_callbackScheduler = new(serviceProvider, Callback, TimeSpan.FromSeconds(1));
            }

            public void ScheduleCallback(DateTime callTime)
            {
                m_callbackScheduler.ScheduleCallback(callTime);
            }

            public void CancelCallback()
            {
                m_callbackScheduler.CancelCallback();
            }

            public async Task WaitCallbackAsync()
            {
                bool success = await m_resetEvent.WaitOneAsync(TimeSpan.FromSeconds(2));
                Assert.True(success, "Reset event not called within wait period");
            }

            private Task Callback()
            {
                ++CallbackCount;
                m_resetEvent.Set();
                return Task.CompletedTask;
            }
        }
    }
}
