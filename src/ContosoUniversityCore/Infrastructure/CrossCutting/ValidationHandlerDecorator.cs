namespace ContosoUniversityCore.Infrastructure.CrossCutting
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentValidation;
    using MediatR;

    public class AsyncValidationHandlerDecorator<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse> where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _inner;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public AsyncValidationHandlerDecorator(IAsyncRequestHandler<TRequest, TResponse> inner, IEnumerable<IValidator<TRequest>> validators)
        {
            _inner = inner;
            _validators = validators;
        }

        public Task<TResponse> Handle(TRequest message)
        {
            var failuers = _validators
               .Select(v => v.Validate(message))
               .SelectMany(result => result.Errors)
               .Where(f => f != null)
               .ToList();

            if (failuers.Any())
                throw new ValidationException(failuers);

            return _inner.Handle(message);
        }
    }

    public class ValidationHandlerDecorator<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _inner;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationHandlerDecorator(IRequestHandler<TRequest, TResponse> inner, IEnumerable<IValidator<TRequest>> validators)
        {
            _inner = inner;
            _validators = validators;
        }
        public TResponse Handle(TRequest message)
        {
            var failuers = _validators
               .Select(v => v.Validate(message))
               .SelectMany(result => result.Errors)
               .Where(f => f != null)
               .ToList();

            if (failuers.Any())
                throw new ValidationException(failuers);

            return _inner.Handle(message);
        }
    }
}