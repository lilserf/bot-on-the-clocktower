using System;

namespace Bot.Api
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base()
        {}
        public UnauthorizedException(Exception innerException)
            : base(innerException.Message, innerException)
        {}
    }
}
