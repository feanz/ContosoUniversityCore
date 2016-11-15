namespace ContosoUniversityCore.Infrastructure.CrossCutting
{
    using System;
    using System.Threading.Tasks;
    using MediatR;

    public class AsyncTransactionHandlerDecorator<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse> where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _inner;
        private readonly SchoolContext _db;

        public AsyncTransactionHandlerDecorator(IAsyncRequestHandler<TRequest, TResponse> inner, SchoolContext db)
        {
            _inner = inner;
            _db = db;
        }

        public async Task<TResponse> Handle(TRequest message)
        {
            try
            {
                _db.BeginTransaction();

                var response = await _inner.Handle(message);

                await _db.CommitTransactionAsync();

                return response;
            }
            catch (Exception)
            {
                _db.RollbackTransaction();
                throw;
            }
        }
    }

    public class TransactionHandlerDecorator<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _inner;
        private readonly SchoolContext _db;

        public TransactionHandlerDecorator(IRequestHandler<TRequest, TResponse> inner, SchoolContext db)
        {
            _inner = inner;
            _db = db;
        }

        public TResponse Handle(TRequest message)
        {
            try
            {
                _db.BeginTransaction();

                var response = _inner.Handle(message);

                _db.CommitTransactionAsync().Wait();

                return response;
            }
            catch (Exception)
            {
                _db.RollbackTransaction();
                throw;
            }
        }
    }
}