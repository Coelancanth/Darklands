using MediatR;
using System.Diagnostics;
using Darklands.Application.Common;

namespace Darklands.Application.Infrastructure.DependencyInjection;

/// <summary>
/// MediatR pipeline behavior that logs all command and query executions.
/// Provides performance metrics and debugging information for the CQRS pipeline.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICategoryLogger _logger;

    public LoggingBehavior(ICategoryLogger logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.Log(LogLevel.Debug, LogCategory.Command, "Executing {0}", requestName);

        try
        {
            var response = await next();

            stopwatch.Stop();
            _logger.Log(LogLevel.Debug, LogCategory.Command, "Completed {0} in {1}ms", requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Log(LogLevel.Error, LogCategory.Command, "Failed {0} after {1}ms: {2}", requestName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
