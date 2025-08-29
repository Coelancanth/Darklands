using MediatR;
using Microsoft.Extensions.Logging;
using LanguageExt;
using LanguageExt.Common;

namespace Darklands.Core.Infrastructure.DependencyInjection;

/// <summary>
/// MediatR pipeline behavior that provides consistent error handling for all commands and queries.
/// Converts exceptions to Fin<T> results and ensures errors are properly logged and contained.
/// </summary>
public class ErrorHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ErrorHandlingBehavior<TRequest, TResponse>> _logger;
    
    public ErrorHandlingBehavior(ILogger<ErrorHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogError(ex, "Unhandled exception in {RequestName}", requestName);
            
            // If TResponse is Fin<T>, convert exception to error result
            if (typeof(TResponse).IsGenericType && 
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Fin<>))
            {
                var errorMethod = typeof(Prelude)
                    .GetMethod(nameof(Prelude.Fail))
                    ?.MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0]);
                    
                if (errorMethod != null)
                {
                    var error = Error.New($"Unhandled exception in {requestName}: {ex.Message}", ex);
                    return (TResponse)errorMethod.Invoke(null, new object[] { error })!;
                }
            }
            
            // Re-throw if we can't convert to error result
            throw;
        }
    }
}