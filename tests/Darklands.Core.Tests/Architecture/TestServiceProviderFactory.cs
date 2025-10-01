using Darklands.Core.Features.Health.Application.Commands;
using Darklands.Core.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Darklands.Core.Tests.Architecture;

/// <summary>
/// Factory for building test ServiceProvider that mirrors Main.cs MediatR configuration.
/// Used by architecture tests to validate Core layer service registration patterns.
/// </summary>
/// <remarks>
/// SCOPE: Core layer services only (MediatR handlers, UIEventForwarder).
/// Presentation layer services (LoggingService, GodotEventBus) are not testable
/// from Core.Tests as they require Godot runtime.
/// </remarks>
internal static class TestServiceProviderFactory
{
    /// <summary>
    /// Builds a ServiceProvider with Core layer services (MediatR configuration from Main.cs).
    /// WHY: Architecture tests must validate MediatR registration patterns (VS_004 post-mortem).
    /// </summary>
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Minimal logging (required by UIEventForwarder constructor dependency)
        services.AddLogging(); // Default configuration (no providers needed for tests)

        // Mock IGodotEventBus (required by UIEventForwarder constructor dependency)
        // NOTE: Actual GodotEventBus implementation is in Presentation layer (requires Godot runtime)
        services.AddSingleton(Substitute.For<IGodotEventBus>());

        // Mirror Main.cs MediatR registration (Main.cs lines 82-87)
        services.AddMediatR(cfg =>
        {
            // Register from Core assembly (same as Main.cs)
            // This finds command handlers AND UIEventForwarder via assembly scan
            cfg.RegisterServicesFromAssembly(typeof(TakeDamageCommand).Assembly);
        });

        // NOTE: GameStrapper.RegisterCoreServices() would normally register additional Core services here
        // For architecture tests, we only need MediatR + logging + mock event bus to validate registration patterns

        return services.BuildServiceProvider();
    }
}
