using CSharpFunctionalExtensions;
using Darklands.Core.Application.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Darklands.Core.Infrastructure.DependencyInjection;

/// <summary>
/// ServiceLocator bridge for Godot boundary pattern.
/// ONLY use in Godot node _Ready() methods - Core code uses constructor injection.
/// NOT an autoload - initialized explicitly in Main scene root per ADR-002.
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// Get service from DI container with functional error handling.
    /// Returns Result&lt;T&gt; for graceful failure handling.
    /// Use this in _Ready() methods when you want to handle initialization failures.
    /// </summary>
    /// <typeparam name="T">Service type to resolve</typeparam>
    /// <returns>Result containing service or error message</returns>
    public static Result<T> GetService<T>() where T : class
    {
        return GameStrapper.GetServices()
            .Bind(provider =>
            {
                try
                {
                    var service = provider.GetService<T>();
                    return service != null
                        ? Result.Success(service)
                        : Result.Failure<T>($"Service {typeof(T).Name} not registered in DI container");
                }
                catch (Exception ex)
                {
                    return Result.Failure<T>($"Failed to resolve {typeof(T).Name}: {ex.Message}");
                }
            });
    }

    /// <summary>
    /// Get service from DI container (fail-fast).
    /// Throws exception if service not available.
    /// Use this in _Ready() methods after bootstrap is verified complete.
    /// </summary>
    /// <typeparam name="T">Service type to resolve</typeparam>
    /// <returns>Service instance</returns>
    /// <exception cref="InvalidOperationException">If DI not initialized or service not registered</exception>
    public static T Get<T>() where T : class
    {
        var result = GetService<T>();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Value;
    }
}
