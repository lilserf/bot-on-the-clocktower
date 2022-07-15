using System;
using System.Threading.Tasks;
using Xunit;

namespace Test.Bot.Base
{
    public static class TestTaskHelper
    {
        public static void AssertCompletedTask(Func<Task> runTask)
        {
            var t = runTask();
            t.Wait(50);
            Assert.True(t.IsCompleted, "Task did not complete in time allotted");
        }

        public static T AssertCompletedTask<T>(Func<Task<T>> runTask)
        {
            var t = runTask();
            t.Wait(50);
            Assert.True(t.IsCompleted, "Task did not complete in time allotted");
            return t.Result;
        }
    }
}
