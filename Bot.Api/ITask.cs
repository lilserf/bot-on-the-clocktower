using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface ITask
    {
        Task Delay(TimeSpan span, CancellationToken ct);
    }

    public static class ITaskExtensions
    {
        public static Task Delay(this ITask @this, int milliseconds)
        {
            return @this.Delay(TimeSpan.FromMilliseconds(milliseconds), CancellationToken.None);
        }

        public static Task Delay(this ITask @this, TimeSpan span)
        {
            return @this.Delay(span, CancellationToken.None);
        }  

        public static Task Delay(this ITask @this, int milliseconds, CancellationToken ct)
        {
            return @this.Delay(TimeSpan.FromMilliseconds(milliseconds), ct);
        }            
    }
}
