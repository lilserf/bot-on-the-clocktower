using Bot.Api;
using Bot.Main;
using System;
using Xunit;

namespace Test.Main
{
    public static class TestProgram
    {
        [Fact]
        public static void CreateServices_HasEnvironment()
        {
            var sp = Program.RegisterServices();
            var env = sp.GetService<IEnvironment>();

            Assert.IsType<ProgramEnvironment>(env);
        }

        [Fact]
        public static void CreateServices_HasDateTime()
        {
            var sp = Program.RegisterServices();
            var env = sp.GetService<IDateTime>();

            Assert.IsType<DateTimeStatic>(env);
        }

        [Fact]
        public static void CreateServices_HasTask()
        {
            var sp = Program.RegisterServices();
            var env = sp.GetService<ITask>();

            Assert.IsType<TaskStatic>(env);
        }
    }
}
