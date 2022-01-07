using Bot.Core;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public static class TestTimeSpanParser
    {
        [Theory]
        [InlineData("")]
        [InlineData("m")]
        [InlineData("min")]
        [InlineData("minutes")]
        [InlineData("s")]
        [InlineData("sec")]
        [InlineData("seconds")]
        [InlineData("1")]
        [InlineData("01")]
        [InlineData(".")]
        [InlineData("0m")]
        public static void TimeSpanParser_InvalidOptions(string test)
        {
            var parsedTimeSpan = TimeSpanParser.Parse(test);
            Assert.Null(parsedTimeSpan);
        }

        [Theory]
        [InlineData(1, "1 m")]
        [InlineData(2, "2 min")]
        [InlineData(3, "3 mins")]
        [InlineData(4, "4 minute")]
        [InlineData(5, "5 minutes")]
        [InlineData(5, "5m")]
        [InlineData(4, "4min")]
        [InlineData(3, "3mins")]
        [InlineData(2, "2minute")]
        [InlineData(1, "1minutes")]
        [InlineData(10, "10m")]
        public static void TimeSpanParser_SimpleMinutes(int mins, string test)
        {
            var expectedTimeSpan = TimeSpan.FromMinutes(mins);
            var parsedTimeSpan = TimeSpanParser.Parse(test);
            Assert.Equal(expectedTimeSpan, parsedTimeSpan);
        }

        [Theory]
        [InlineData("-1m")]
        [InlineData("1m 2m")]
        [InlineData("1m2minutes")]
        [InlineData("1m 2min")]
        public static void TimeSpanParser_InvalidMinutes(string test)
        {
            var parsedTimeSpan = TimeSpanParser.Parse(test);
            Assert.Null(parsedTimeSpan);
        }

        [Theory]
        [InlineData(1, "1 s")]
        [InlineData(2, "2 sec")]
        [InlineData(3, "3 secs")]
        [InlineData(4, "4 second")]
        [InlineData(5, "5 seconds")]
        [InlineData(5, "5s")]
        [InlineData(4, "4sec")]
        [InlineData(3, "3secs")]
        [InlineData(2, "2second")]
        [InlineData(1, "1seconds")]
        [InlineData(10, "10s")]
        public static void TimeSpanParser_SimpleSeconds(int secs, string test)
        {
            var expectedTimeSpan = TimeSpan.FromSeconds(secs);
            var parsedTimeSpan = TimeSpanParser.Parse(test);
            Assert.Equal(expectedTimeSpan, parsedTimeSpan);
        }

        [Theory]
        [InlineData("-1s")]
        [InlineData("1s 2s")]
        [InlineData("1s2seconds")]
        [InlineData("1s 2sec")]
        public static void TimeSpanParser_InvalidSeconds(string test)
        {
            var parsedTimeSpan = TimeSpanParser.Parse(test);
            Assert.Null(parsedTimeSpan);
        }

        [Theory]
        [InlineData(1, 2, "1m2s")]
        [InlineData(5, 30, "30 sec, 5 mins")]
        [InlineData(2, 0, "120 sec")]
        [InlineData(1, 45, "1 minute    45 seconds")]
        public static void TimeSpanParser_MinsAndSecs(int mins, int secs, string test)
        {
            var expectedTimeSpan = TimeSpan.FromMinutes(mins) + TimeSpan.FromSeconds(secs);
            var parsedTimeSpan = TimeSpanParser.Parse(test);
            Assert.Equal(expectedTimeSpan, parsedTimeSpan);
        }
    }
}
