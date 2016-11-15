namespace ContosoUniversityCore.Infrastructure.CrossCutting
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using MediatR;
    using Microsoft.Extensions.Logging;

    public class AsyncMetricsHandlerDecorator<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse> where TRequest : IAsyncRequest<TResponse>
    {
        private readonly IAsyncRequestHandler<TRequest, TResponse> _inner;
        private readonly ILogger _logger;

        public AsyncMetricsHandlerDecorator(IAsyncRequestHandler<TRequest, TResponse> inner, ILoggerFactory loggerFactory)
        {
            _inner = inner;
            _logger = loggerFactory.CreateLogger("Metrics");
        }

        public Task<TResponse> Handle(TRequest message)
        {
            var name = typeof(TRequest).FullName;
            using (Metrics.Time(name, elapsed => _logger.LogInformation($"{name} executed, time elapsed {elapsed}")))
            {
                return _inner.Handle(message);
            }
        }
    }

    public class MetricsHandlerDecorator<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _inner;
        private readonly ILogger _logger;

        public MetricsHandlerDecorator(IRequestHandler<TRequest, TResponse> inner, ILoggerFactory loggerFactory)
        {
            _inner = inner;
            _logger = loggerFactory.CreateLogger("Metrics");
        }

        public TResponse Handle(TRequest message)
        {
            var name = typeof(TRequest).FullName;
            using (Metrics.Time(name, elapsed => _logger.LogInformation($"{name} executed, time elapsed {elapsed}")))
            {
                return _inner.Handle(message);
            }
        }
    }

    public class Metrics : IDisposable
    {
        private readonly string _description;
        private readonly Action<TimeSpan> _onEnd;
        private readonly Stopwatch _watch;

        public Metrics(string description, Action<TimeSpan> onEnd = null)
        {
            _description = description;
            _onEnd = onEnd;
            _watch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _watch.Stop();
            _onEnd(_watch.Elapsed);
            Console.WriteLine($"{_description} executed, time elapsed {_watch.Elapsed}");
        }

        public static Metrics Time(string description, Action<TimeSpan> onEnd = null)
        {
            return new Metrics(description, onEnd);
        }
    }
}