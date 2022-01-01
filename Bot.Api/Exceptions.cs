using System;

namespace Bot.Api
{
    public class BadRequestException : Exception
    {
        public BadRequestException() : base() {}
        public BadRequestException(Exception innerException) : base(innerException.Message, innerException) {}
    }

    public class NotFoundException : Exception
    {
        public NotFoundException() : base() {}
        public NotFoundException(Exception innerException) : base(innerException.Message, innerException) {}
    }

    public class RateLimitException : Exception
    {
        public RateLimitException() : base() {}
        public RateLimitException(Exception innerException) : base(innerException.Message, innerException) {}
    }

    public class RequestSizeException : Exception
    {
        public RequestSizeException() : base() {}
        public RequestSizeException(Exception innerException) : base(innerException.Message, innerException) {}
    }

    public class ServerErrorException : Exception
    {
        public ServerErrorException() : base() {}
        public ServerErrorException(Exception innerException) : base(innerException.Message, innerException) {}
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base() { }
        public UnauthorizedException(Exception innerException) : base(innerException.Message, innerException) { }
    }
}
