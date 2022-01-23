using System;

namespace Bot.Api
{
    public class BadRequestException : Exception
    {
        public BadRequestException() : base() {}
        public BadRequestException(Exception innerException, string extraInfo) : base($"{innerException.Message} : {extraInfo}", innerException) {}
    }

    public class NotFoundException : Exception
    {
        public NotFoundException() : base() {}
        public NotFoundException(Exception innerException, string extraInfo) : base($"{innerException.Message} : {extraInfo}", innerException) {}
    }

    public class RateLimitException : Exception
    {
        public RateLimitException() : base() {}
        public RateLimitException(Exception innerException, string extraInfo) : base($"{innerException.Message} : {extraInfo}", innerException) {}
    }

    public class RequestSizeException : Exception
    {
        public RequestSizeException() : base() {}
        public RequestSizeException(Exception innerException, string extraInfo) : base($"{innerException.Message} : {extraInfo}", innerException) {}
    }

    public class ServerErrorException : Exception
    {
        public ServerErrorException() : base() {}
        public ServerErrorException(Exception innerException, string extraInfo) : base($"{innerException.Message} : {extraInfo}", innerException) {}
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base() { }
        public UnauthorizedException(Exception innerException, string extraInfo) : base($"{innerException.Message} : {extraInfo}", innerException) { }
    }
}
