using System.Collections.Generic;
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

        // Create shared category filter set for LoggingService
        var enabledCategories = new HashSet<string>();

        // Create level switch for runtime log level control
        var levelSwitch = new Serilog.Core.LoggingLevelSwitch(Serilog.Events.LogEventLevel.Information);

        // Configure Serilog with category-based filtering
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .Filter.ByIncludingOnly(logEvent =>
            {
                // Extract category from SourceContext property
                if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue))
                {
                    var sourceContext = sourceContextValue.ToString().Trim('"');
                    var category = LoggingService.ExtractCategory(sourceContext);

                    // If no categories enabled, show all logs (permissive default)
                    if (enabledCategories.Count == 0)
                        return true;

                    // Otherwise, only show logs from enabled categories
                    return enabledCategories.Contains(category ?? "Infrastructure");
                }

                // No SourceContext = Infrastructure log (GameStrapper, ServiceLocator, etc.)
                return enabledCategories.Count == 0 || enabledCategories.Contains("Infrastructure");
            })
            .WriteTo.Sink(new GodotConsoleSink(new GodotConsoleFormatter()))
            .WriteTo.File("logs/darklands.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Add Microsoft.Extensions.Logging with Serilog provider
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });

        // Register LoggingService for category-based filtering (used by DebugConsole)
        services.AddSingleton(new LoggingService(enabledCategories));

        // Register LoggingLevelSwitch for runtime log level control (used by DebugConsole)
        services.AddSingleton(levelSwitch);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 2. MEDIATR - Register MediatR core + command handlers from Core assembly
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // CRITICAL: Scan Core assembly for command handlers (e.g., TakeDamageCommandHandler)
        // DOUBLE REGISTRATION FIX (VS_004 post-mortem):
        // - Assembly scan finds UIEventForwarder (it's in Core assembly)
        // - Open generic registration (line 95) adds UIEventForwarder again
        // - Result: TWO UIEventForwarder instances = duplicate events!
        // - Solution: Use ONLY open generic registration, skip assembly scan for UIEventForwarder
        services.AddMediatR(cfg =>
        {
            // Register command handlers from Core assembly
            // NOTE: This ALSO registers UIEventForwarder via assembly scan
            cfg.RegisterServicesFromAssembly(typeof(Darklands.Core.Features.Health.Application.Commands.TakeDamageCommand).Assembly);
        });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 3. EVENT BUS (VS_004) - GodotEventBus + UIEventForwarder bridge
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Register GodotEventBus as singleton (shared state across all nodes)
        services.AddSingleton<IGodotEventBus, GodotEventBus>();

        // REMOVED: Duplicate UIEventForwarder registration (already registered by assembly scan above)
        // services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        GD.Print("📦 Services registered:");
        GD.Print("   - Logging (Serilog → Console + File)");
        GD.Print("   - LoggingService (category filtering for DebugConsole)");
        GD.Print("   - MediatR (command handlers + UIEventForwarder via assembly scan)");
        GD.Print("   - IGodotEventBus → GodotEventBus");
    }
}