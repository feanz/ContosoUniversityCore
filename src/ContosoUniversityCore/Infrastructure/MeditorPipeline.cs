namespace ContosoUniversityCore.Infrastructure
{
    using System.Threading.Tasks;
    using MediatR;

    public class MediatorPipeline<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse> where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _inner;

        public MediatorPipeline(IAsyncRequestHandler<TRequest, TResponse> inner)
        {
            _inner = inner;
        }

        public Task<TResponse> Handle(TRequest message)
        {
            return _inner.Handle(message);
        }
    }
}