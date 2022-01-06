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

        private DateTime m_currentTime = s_defaultDateTimeNow;


        public TestCallbackScheduler()
        {
            RegisterMock(MockDateTime);
            RegisterMock(MockTask);

            MockDateTime.SetupGet(dt => dt.Now).Returns(() => m_currentTime);
            MockTask.Setup(t => t.Delay(It.IsAny<TimeSpan>())).Returns<TimeSpan>((ts) => Task.Delay(1));
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
        public void CallbackScheduler_ScheduleCallback_NotCalledYet()
        {
            uint key = 7;
            uint cbCount = 0;
            Task cb(uint k)
            {
                Assert.Equal(key, k);
                ++cbCount;
                return Task.CompletedTask;
            }
            CallbackScheduler<uint> cs = new(GetServiceProvider(), cb, TimeSpan.FromSeconds(1));

            cs.ScheduleCallback(key, GetTimeAfter(TimeSpan.FromSeconds(5)));

            Assert.Equal(0u, cbCount);
        }

        [Fact]
        public async Task CallbackScheduler_ScheduleCallback_CalledAfterDelay()
        {
            uint key = 7;
            uint cbCount = 0;
            Task cb(uint k)
            {
                Assert.Equal(key, k);
                ++cbCount;
                return Task.CompletedTask;
            }
            CallbackScheduler<uint> cs = new(GetServiceProvider(), cb, TimeSpan.FromSeconds(1));

            cs.ScheduleCallback(key, GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            await Task.Delay(5);
            Assert.Equal(0u, cbCount);
            AdvanceTime(TimeSpan.FromSeconds(2));
            await Task.Delay(5);
            Assert.Equal(0u, cbCount);
            AdvanceTime(TimeSpan.FromSeconds(10));
            await Task.Delay(5);

            MockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1u, cbCount);
        }

        [Fact]
        public async Task CallbackScheduler_CancelCallback_NeverCalled()
        {
            uint key = 7;
            uint cbCount = 0;
            Task cb(uint k)
            {
                Assert.Equal(key, k);
                ++cbCount;
                return Task.CompletedTask;
            }
            CallbackScheduler<uint> cs = new(GetServiceProvider(), cb, TimeSpan.FromSeconds(1));

            cs.ScheduleCallback(key, GetTimeAfter(TimeSpan.FromSeconds(5)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            await Task.Delay(2);

            cs.CancelCallback(key);

            MockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            MockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);

            AdvanceTime(TimeSpan.FromSeconds(10));
            await Task.Delay(2);

            Assert.Equal(0u, cbCount);
        }

        [Fact]
        public async Task CallbackScheduler_ChangeCallTime_UsesNewValue()
        {
            uint key = 7;
            uint cbCount = 0;
            Task cb(uint k)
            {
                Assert.Equal(key, k);
                ++cbCount;
                return Task.CompletedTask;
            }
            CallbackScheduler<uint> cs = new(GetServiceProvider(), cb, TimeSpan.FromSeconds(1));

            cs.ScheduleCallback(key, GetTimeAfter(TimeSpan.FromSeconds(5)));
            AdvanceTime(TimeSpan.FromSeconds(2));
            await Task.Delay(2);
            Assert.Equal(0u, cbCount);

            cs.ScheduleCallback(key, GetTimeAfter(TimeSpan.FromSeconds(10)));

            AdvanceTime(TimeSpan.FromSeconds(2));
            await Task.Delay(2);
            Assert.Equal(0u, cbCount);

            AdvanceTime(TimeSpan.FromSeconds(10));
            await Task.Delay(2);

            MockTask.Verify(t => t.Delay(It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(1))), Times.AtLeastOnce);
            MockDateTime.VerifyGet(dt => dt.Now, Times.AtLeastOnce);
            Assert.Equal(1u, cbCount);
        }
        [Fact]
        public async Task CallbackScheduler_NoCallbacks_NeverTicked()
        {
            uint key = 7;
            uint cbCount = 0;
            Task cb(uint k)
            {
                Assert.Equal(key, k);
                ++cbCount;
                return Task.CompletedTask;
            }
            CallbackScheduler<uint> cs = new(GetServiceProvider(), cb, TimeSpan.FromSeconds(1));

            AdvanceTime(TimeSpan.FromSeconds(20));
            await Task.Delay(10);

            MockDateTime.VerifyGet(dt => dt.Now, Times.Never);
            MockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CallbackScheduler_FiredOnce_OnlyTickedOnce()
        {
            uint key = 7;
            uint cbCount = 0;
            Task cb(uint k)
            {
                Assert.Equal(key, k);
                ++cbCount;
                return Task.CompletedTask;
            }
            CallbackScheduler<uint> cs = new(GetServiceProvider(), cb, TimeSpan.FromSeconds(1));

            cs.ScheduleCallback(key, GetTimeAfter(TimeSpan.FromSeconds(2)));
            AdvanceTime(TimeSpan.FromSeconds(20));
            await Task.Delay(40);

            Assert.Equal(1u, cbCount);
            MockDateTime.VerifyGet(dt => dt.Now, Times.Once);
            MockTask.Verify(t => t.Delay(It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
