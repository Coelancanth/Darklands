using MediatR;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Debug;

namespace Darklands.Core.Infrastructure.DependencyInjection;

/// <summary>
/// MediatR pipeline behavior that provides consistent error handling for all commands and queries.
/// Converts exceptions to Fin<T> results and ensures errors are properly logged and contained.
/// </summary>
public class ErrorHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICategoryLogger _logger;

    public ErrorHandlingBehavior(ICategoryLogger logger)
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
            _logger.Log(LogLevel.Error, LogCategory.Command, "Unhandled exception in {0}: {1}", requestName, ex.Message);

            // If TResponse is Fin<T>, convert exception to error result
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Fin<>))
            {
                var errorMethod = typeof(Prelude)
                    .GetMethod(nameof(Prelude.Fail), new[] { typeof(Error) })
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
