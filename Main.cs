using Godot;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Infrastructure.Events;
using Darklands.Infrastructure.Events;
using Darklands.Infrastructure.Logging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Darklands;

/// <summary>
/// Main entry point for Darklands.
/// Initializes DI container with EventBus, Logging, and MediatR.
/// </summary>
public partial class Main : Node
{
    public override void _Ready()
    {
        GD.Print("🎮 Darklands - Initializing...");

        // Initialize DI container with all infrastructure services
        var result = GameStrapper.Initialize(ConfigureServices);

        if (result.IsFailure)
        {
            GD.PrintErr($"❌ Failed to initialize DI container: {result.Error}");
            GetTree().Quit();
            return;
        }

        GD.Print("✅ DI Container initialized successfully");
        GD.Print("✅ EventBus ready");
        GD.Print("✅ Logging configured");
        GD.Print("✅ MediatR registered");
        GD.Print("");
        GD.Print("🎯 Ready to load scenes!");
    }

    /// <summary>
    /// Configure Presentation layer services (Logging, EventBus, MediatR).
    /// Called by GameStrapper.Initialize().
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 1. LOGGING (VS_003) - Serilog with category-based filtering
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/darklands.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Add Microsoft.Extensions.Logging with Serilog provider
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 2. MEDIATR - Register MediatR core (NOT handlers - open generics handle that)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // CRITICAL: Do NOT scan for handlers with RegisterServicesFromAssembly(typeof(TestEvent).Assembly)
        // WHY: Would register UIEventForwarder twice (assembly scan + open generic) → events published twice
        // SOLUTION: Only scan MediatR's own assembly for core types (IMediator, etc.)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(IMediator).Assembly));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 3. EVENT BUS (VS_004) - GodotEventBus + UIEventForwarder bridge
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Register GodotEventBus as singleton (shared state across all nodes)
        services.AddSingleton<IGodotEventBus, GodotEventBus>();

        // Register UIEventForwarder with OPEN GENERICS (zero boilerplate!)
        // MediatR will auto-resolve UIEventForwarder<TEvent> for ANY INotification
        services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        GD.Print("📦 Services registered:");
        GD.Print("   - Logging (Serilog → Console + File)");
        GD.Print("   - MediatR (IMediator only, handlers via open generics)");
        GD.Print("   - IGodotEventBus → GodotEventBus");
        GD.Print("   - UIEventForwarder<T> (open generic registration)");
    }
}