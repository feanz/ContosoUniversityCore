namespace ContosoUniversityCore.Infrastructure.CrossCutting
{
    using System.Threading.Tasks;
    using MediatR;
    using Serilog.Context;
   
    public class AsyncLoggingHandlerDecorator<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse> where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _inner;

        public AsyncLoggingHandlerDecorator(IAsyncRequestHandler<TRequest, TResponse> inner)
        {
            _inner = inner;
        }

        public Task<TResponse> Handle(TRequest message)
        {
            using (LogContext.PushProperty(LogConstants.MediatRRequestType, typeof(TRequest).FullName))
            {
                return _inner.Handle(message);
            }
        }
    }

    public class LoggingHandlerDecorator<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _inner;

        public LoggingHandlerDecorator(IRequestHandler<TRequest, TResponse> inner)
        {
            _inner = inner;
        }

        public TResponse Handle(TRequest message)
        {
            using (LogContext.PushProperty(LogConstants.MediatRRequestType, typeof(TRequest).FullName))
            {
                return _inner.Handle(message);
            }
        }
    }

    static class LogConstants
    {
        public const string MediatRRequestType = "MediatRRequestType";
    }
}