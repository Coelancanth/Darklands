using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Darklands.Core.Infrastructure.DependencyInjection;

/// <summary>
/// MediatR pipeline behavior that logs all command and query executions.
/// Provides performance metrics and debugging information for the CQRS pipeline.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
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
        
        _logger.LogDebug("Executing {RequestName}", requestName);
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            _logger.LogDebug("Completed {RequestName} in {ElapsedMs}ms", 
                requestName, stopwatch.ElapsedMilliseconds);
                
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed {RequestName} after {ElapsedMs}ms", 
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}