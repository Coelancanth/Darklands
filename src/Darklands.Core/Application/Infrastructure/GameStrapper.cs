using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace Darklands.Core.Application.Infrastructure;

/// <summary>
/// Bootstraps the DI container for the application.
/// Called once during application startup (typically in Main scene root).
/// </summary>
public static class GameStrapper
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Initialize the DI container. Idempotent - safe to call multiple times.
    /// Returns Success if already initialized or initialization succeeds.
    /// </summary>
    /// <param name="configureServices">Optional action to configure additional services from Presentation layer (e.g., logging)</param>
    public static Result Initialize(Action<IServiceCollection>? configureServices = null)
    {
        lock (_lock)
        {
            // Idempotent: return success if already initialized
            if (_serviceProvider != null)
            {
                return Result.Success();
            }

            try
            {
                var services = new ServiceCollection();

                // Register core services
                RegisterCoreServices(services);

                // Allow Presentation layer to configure additional services
                // (e.g., Serilog logging, which requires packages not in Core)
                configureServices?.Invoke(services);

                // Build service provider
                _serviceProvider = services.BuildServiceProvider();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to initialize DI container: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get the service provider. Returns Failure if not initialized.
    /// </summary>
    public static Result<IServiceProvider> GetServices()
    {
        lock (_lock)
        {
            return _serviceProvider != null
                ? Result.Success(_serviceProvider)
                : Result.Failure<IServiceProvider>("DI container not initialized. Call GameStrapper.Initialize() first.");
        }
    }

    /// <summary>
    /// Register all core services with the DI container.
    /// </summary>
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // Temporary test service for VS_002 validation
        // TODO: Remove after VS_001 (health system) is complete
        services.AddSingleton<ITestService, TestService>();

        // Future services will be registered here:
        // - Logging (VS_003)
        // - Event Bus (VS_004)
        // - Component Registry, etc.
    }

    /// <summary>
    /// Reset the service provider. ONLY for testing.
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _serviceProvider = null;
        }
    }
}

/// <summary>
/// Temporary test service for VS_002 validation.
/// Will be removed after VS_001 (health system) is complete.
/// </summary>
public interface ITestService
{
    string GetTestMessage();
}

/// <summary>
/// Simple implementation for DI validation.
/// </summary>
internal class TestService : ITestService
{
    public string GetTestMessage() => "DI is working!";
}
