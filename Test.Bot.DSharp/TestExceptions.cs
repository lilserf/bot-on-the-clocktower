using Bot.Api;
using Bot.DSharp;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.DSharp
{
    public class TestExceptions : TestBase
    {
        [Fact]
        public static void WrapExceptions_NoThrow_Continues()
        {
            var t = ExceptionWrap.WrapExceptionsAsync(() => Task.CompletedTask);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }

        /* Commented out so it doesn't show up in the Test Explorer
        [Theory(Skip="The DSharpPlus exceptions do not have public constructors so I cannot figure out how to run a test that relies on us throwing those exceptions during the test.")]
        [InlineData(typeof(DSharpPlus.Exceptions.BadRequestException), typeof(BadRequestException))]
        [InlineData(typeof(DSharpPlus.Exceptions.NotFoundException), typeof(NotFoundException))]
        [InlineData(typeof(DSharpPlus.Exceptions.RateLimitException), typeof(RateLimitException))]
        [InlineData(typeof(DSharpPlus.Exceptions.RequestSizeException), typeof(RequestSizeException))]
        [InlineData(typeof(DSharpPlus.Exceptions.ServerErrorException), typeof(ServerErrorException))]
        [InlineData(typeof(DSharpPlus.Exceptions.UnauthorizedException), typeof(UnauthorizedException))]
        public static void WrapExceptions_ThrowsDSharpException_ThrowsProperApiException(Type dSharpException, Type apiException)
        {
            var constructor = dSharpException.GetConstructor(Array.Empty<Type>());
            Assert.NotNull(constructor);
            Task throwFunc() => throw (Exception)(constructor!.Invoke(Array.Empty<object?>()));
            var t = Assert.ThrowsAsync(apiException, () => ExceptionWrap.WrapExceptionsAsync(throwFunc));
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }
        */
    }
}
