using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public static class ExceptionWrap
    {
        public static Task WrapExceptionsAsync(Func<Task> f) => WrapExceptionsAsync( async () => { await f(); return true; });

        public static async Task<T> WrapExceptionsAsync<T>(Func<Task<T>> f)
        {
            try
            {
                return await f();
            }
            catch (DSharpPlus.Exceptions.BadRequestException e)
            {
                throw new Api.BadRequestException(e);
            }
            catch (DSharpPlus.Exceptions.NotFoundException e)
            {
                throw new Api.NotFoundException(e);
            }
            catch (DSharpPlus.Exceptions.RateLimitException e)
            {
                throw new Api.RateLimitException(e);
            }
            catch (DSharpPlus.Exceptions.RequestSizeException e)
            {
                throw new Api.RequestSizeException(e);
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                throw new Api.ServerErrorException(e); 
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException e)
            {
                throw new Api.UnauthorizedException(e);
            }
        }
    }
}
