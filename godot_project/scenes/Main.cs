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
        GD.Print("Darklands - Initializing...");

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
        GD.Print("Ready to load scenes!");
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 4. TERRAIN SYSTEM (VS_019 Phase 2) - TileSet-based terrain catalog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Load terrain TileSet resource
        var terrainTileSet = GD.Load<TileSet>("res://assets/micro-roguelike/test_terrain_tileset.tres");

        if (terrainTileSet == null)
        {
            GD.PrintErr("❌ Failed to load terrain TileSet: res://assets/micro-roguelike/test_terrain_tileset.tres");
        }
        else
        {
            // Register TileSetTerrainRepository with loaded TileSet
            services.AddSingleton<Darklands.Core.Features.Grid.Application.ITerrainRepository>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Infrastructure.TileSetTerrainRepository>>();
                return new Infrastructure.TileSetTerrainRepository(terrainTileSet, logger);
            });

            GD.Print("   - ITerrainRepository → TileSetTerrainRepository (test_terrain_tileset.tres loaded)");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 5. ITEM SYSTEM (VS_009 Phase 4) - TileSet-based item catalog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Load item TileSet resource
        var itemTileSet = GD.Load<TileSet>("res://assets/inventory_ref/item_sprites.tres");

        if (itemTileSet == null)
        {
            GD.PrintErr("❌ Failed to load item TileSet: res://assets/inventory_ref/item_sprites.tres");
        }
        else
        {
            // Register TileSetItemRepository with loaded TileSet
            services.AddSingleton<Darklands.Core.Features.Item.Application.IItemRepository>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Infrastructure.TileSetItemRepository>>();
                return new Infrastructure.TileSetItemRepository(itemTileSet, logger);
            });

            GD.Print("   - IItemRepository → TileSetItemRepository (item_sprites.tres loaded)");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 6. TEMPLATE SYSTEM (VS_021 Phase 2) - Data-driven entity templates
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Register ActorTemplate service (loads .tres files from data/entities/)
        services.AddSingleton<Darklands.Core.Infrastructure.Templates.ITemplateService<Infrastructure.Templates.ActorTemplate>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Infrastructure.Templates.GodotTemplateService<Infrastructure.Templates.ActorTemplate>>>();
            var templateService = new Infrastructure.Templates.GodotTemplateService<Infrastructure.Templates.ActorTemplate>(
                logger,
                "res://data/entities/"
            );

            // Load templates at startup (fail-fast if any template invalid)
            var loadResult = templateService.LoadTemplates();
            if (loadResult.IsFailure)
            {
                GD.PrintErr($"❌ Failed to load actor templates: {loadResult.Error}");
                throw new System.InvalidOperationException($"Template loading failed: {loadResult.Error}");
            }

            GD.Print($"   - ITemplateService<ActorTemplate> → GodotTemplateService ({templateService.GetAllTemplates().Count} templates loaded)");
            return templateService;
        });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 7. WORLDGEN SYSTEM (VS_019 Phase 3) - Plate tectonics world generation
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // Override Core's IPlateSimulator registration with correct Godot project path
        // (Core uses Directory.GetCurrentDirectory() which doesn't work in Godot)
        var projectPath = ProjectSettings.GlobalizePath("res://");
        services.AddSingleton<Darklands.Core.Features.WorldGen.Application.Abstractions.IPlateSimulator>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Darklands.Core.Features.WorldGen.Infrastructure.Native.NativePlateSimulator>>();
            return new Darklands.Core.Features.WorldGen.Infrastructure.Native.NativePlateSimulator(logger, projectPath);
        });

        GD.Print($"   - IPlateSimulator → NativePlateSimulator (projectPath: {projectPath})");

        GD.Print("Services registered:");
        GD.Print("   - Logging (Serilog → Console + File)");
        GD.Print("   - LoggingService (category filtering for DebugConsole)");
        GD.Print("   - MediatR (command handlers + UIEventForwarder via assembly scan)");
        GD.Print("   - IGodotEventBus → GodotEventBus");
        GD.Print("   - ITerrainRepository → TileSetTerrainRepository (auto-discovery from TileSet)");
        GD.Print("   - IItemRepository → TileSetItemRepository (auto-discovery from TileSet)");
        GD.Print("   - ITemplateService<ActorTemplate> → GodotTemplateService (data-driven entities)");
        GD.Print("   - IPlateSimulator → NativePlateSimulator (plate tectonics world generation)");
    }
}