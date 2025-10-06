using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
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

        // Player Context (VS_007 Phase 3) - Provides player character identity
        services.AddSingleton<Darklands.Core.Application.IPlayerContext,
            Darklands.Core.Infrastructure.PlayerContext>();

        // MediatR (VS_003) - Registered via Presentation layer configureServices callback

        // Event Bus (VS_004) - Infrastructure must be registered in Presentation
        // Core only has IGodotEventBus interface and UIEventForwarder
        // Presentation registers: GodotEventBus, MediatR assembly scan, UIEventForwarder open generic

        // Health System (VS_001 Phase 3) - Application services
        services.AddSingleton<Features.Health.Application.IHealthComponentRegistry,
            Features.Health.Infrastructure.HealthComponentRegistry>();

        // Grid System (VS_005 Phase 3+4, VS_019 Phase 2) - GridMap factory, FOV service, position service
        // NOTE: ITerrainRepository is registered in Main.cs (Presentation layer loads TileSet)
        services.AddSingleton<Features.Grid.Domain.GridMap>(provider =>
        {
            var terrainRepo = provider.GetRequiredService<Features.Grid.Application.ITerrainRepository>();
            var defaultTerrain = terrainRepo.GetDefault().Value;  // "floor" terrain from TileSet
            return new Features.Grid.Domain.GridMap(defaultTerrain);
        });
        services.AddSingleton<Features.Grid.Application.Services.IFOVService,
            Features.Grid.Infrastructure.Services.ShadowcastingFOVService>();
        services.AddSingleton<Features.Grid.Application.Services.IActorPositionService,
            Features.Grid.Infrastructure.Services.ActorPositionService>();

        // Movement System (VS_006 Phase 3+4) - Pathfinding service
        services.AddSingleton<Features.Movement.Application.Services.IPathfindingService,
            Features.Movement.Infrastructure.Services.AStarPathfindingService>();

        // Inventory System (VS_008 Phase 3) - Repository
        services.AddSingleton<Features.Inventory.Application.IInventoryRepository,
            Features.Inventory.Infrastructure.InMemoryInventoryRepository>();

        // Combat System (VS_007 Phase 3) - Turn queue repository
        services.AddSingleton<Features.Combat.Application.ITurnQueueRepository,
            Features.Combat.Infrastructure.InMemoryTurnQueueRepository>();

        // Actor System (VS_020 Phase 0) - Actor repository
        services.AddSingleton<IActorRepository,
            Darklands.Core.Infrastructure.Repositories.InMemoryActorRepository>();

        // Template System (VS_021 Phase 2) - Data-driven entity templates
        // NOTE: Actual template loading registered in Presentation layer (Main.cs)
        // because ActorTemplate (Godot Resource) lives in Presentation (Godot SDK project)
        // Core only knows about ITemplateService abstraction (Godot-free!)

        // WorldGen System (VS_019 Phase 2-3) - Plate tectonics world generation
        // NOTE: projectPath will be provided by Presentation layer via factory
        // (Core can't access Godot's ProjectSettings, so Presentation resolves path)
        services.AddSingleton<Features.WorldGen.Application.Abstractions.IPlateSimulator>(provider =>
        {
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<
                Features.WorldGen.Infrastructure.Native.NativePlateSimulator>>();

            // Project path must be injected from Presentation layer
            // For now, use working directory (will be overridden by Presentation)
            var projectPath = System.IO.Directory.GetCurrentDirectory();

            return new Features.WorldGen.Infrastructure.Native.NativePlateSimulator(logger, projectPath);
        });
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
public class TestService : ITestService
{
    public string GetTestMessage() => "DI is working!";
}
