using System.Collections.Generic;
using Godot;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Darklands.TestScenes;

/// <summary>
/// Manual validation scene for VS_002 Phase 3.
/// Tests that DI container initializes correctly and services can be resolved.
/// </summary>
public partial class DIBootstrapTest : Node2D
{
    private Label? _statusLabel;
    private Button? _testButton;
    private ILogger<DIBootstrapTest>? _logger;
    private int _clickCount = 0;

    public override void _Ready()
    {
        // PRE-DI: Use GD.Print (ServiceLocator not ready yet)
        GD.Print("=== DI Bootstrap Test Scene Loading ===");

        // Get nodes by path instead of Export (more reliable)
        _statusLabel = GetNode<Label>("StatusLabel");
        _testButton = GetNode<Button>("TestButton");

        // Configure Serilog with category filtering BEFORE initializing DI container
        // NOTE: This is Presentation layer code - Serilog packages are only in Darklands.csproj

        // Create shared state for category filtering
        var enabledCategories = new HashSet<string>
        {
            "Combat", "Movement", "AI", "Infrastructure", "Network"
        };

        // Create Godot sinks
        // 1. GodotConsoleSink: Writes to Godot's Output panel (bottom console)
        var godotConsoleFormatter = new Darklands.Infrastructure.Logging.GodotConsoleFormatter();
        var godotConsoleSink = new Darklands.Infrastructure.Logging.GodotConsoleSink(godotConsoleFormatter);

        // Configure Serilog with TWO sinks: Godot Output panel + File
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()

            // ===== Category Filter (checks once before sinks) =====
            .Filter.ByIncludingOnly(logEvent =>
            {
                if (logEvent.Properties.TryGetValue("SourceContext", out var ctx))
                {
                    var fullName = ctx.ToString().Trim('"');
                    var category = Darklands.Infrastructure.Logging.LoggingService.ExtractCategory(fullName);
                    return category != null && enabledCategories.Contains(category);
                }
                return true;  // Include logs without SourceContext
            })

            // Sink 1: Godot's Output panel (controlled by category filters)
            .WriteTo.Sink(godotConsoleSink)

            // Sink 2: File logging (always logs everything for historical reference)
            .WriteTo.File(
                "logs/darklands.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext:l}: {Message:lj}{NewLine}",
                shared: true  // Allow concurrent reads (for tail -f)
            )
            .CreateLogger();

        // PRE-DI: Using GD.Print here because logger itself is being configured
        GD.Print("✅ Serilog configured (Godot Output panel + File logging with category filtering)");

        // Initialize DI container with logging configuration
        var initResult = GameStrapper.Initialize(services =>
        {
            // Bridge Serilog → Microsoft.Extensions.Logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger, dispose: true);
            });

            // Register LoggingService for runtime category control
            services.AddSingleton(new Darklands.Infrastructure.Logging.LoggingService(enabledCategories));
        });

        if (initResult.IsSuccess)
        {
            // POST-DI: Now we can use ILogger
            _logger = ServiceLocator.Get<ILogger<DIBootstrapTest>>();
            _logger.LogInformation("GameStrapper initialized successfully");
            UpdateStatus("DI Container: Initialized ✅", Colors.Green);
        }
        else
        {
            // CRITICAL ERROR: Logger unavailable, use GD.PrintErr
            GD.PrintErr($"❌ GameStrapper.Initialize() failed: {initResult.Error}");
            UpdateStatus($"DI Container: FAILED - {initResult.Error}", Colors.Red);
            return;
        }

        // Verify service resolution
        var serviceResult = ServiceLocator.GetService<ITestService>();

        if (serviceResult.IsSuccess)
        {
            _logger.LogInformation("ServiceLocator resolved ITestService: {Message}", serviceResult.Value.GetTestMessage());
        }
        else
        {
            _logger.LogError("ServiceLocator failed to resolve ITestService: {Error}", serviceResult.Error);
        }

        // ===== VS_003 Phase 2: Test category filtering =====
        var loggingService = ServiceLocator.Get<Darklands.Infrastructure.Logging.LoggingService>();

        _logger.LogInformation("Testing Category Filtering (VS_003 Phase 2)");
        _logger.LogDebug("Initial enabled categories: {Categories}", string.Join(", ", loggingService.GetEnabledCategories()));

        // Test disabling a category
        loggingService.DisableCategory("Combat");
        _logger.LogDebug("Disabled 'Combat' category");
        _logger.LogDebug("Enabled categories after disable: {Categories}", string.Join(", ", loggingService.GetEnabledCategories()));

        // Test re-enabling
        loggingService.EnableCategory("Combat");
        _logger.LogDebug("Re-enabled 'Combat' category");
        _logger.LogDebug("Enabled categories after enable: {Categories}", string.Join(", ", loggingService.GetEnabledCategories()));

        // Test category extraction
        var testCategory = Darklands.Infrastructure.Logging.LoggingService.ExtractCategory(
            "Darklands.Core.Application.Commands.Combat.ExecuteAttackCommandHandler");
        _logger.LogDebug("Extracted category 'Combat' from handler name: {Category}", testCategory);

        // ===== VS_003 Phase 3: Test Godot Console Sink =====
        _logger.LogInformation("Testing Godot Console Sink (VS_003 Phase 3)");

        // Emit test logs at different levels to demonstrate color formatting
        _logger.LogDebug("This is a DEBUG message (gray) - low importance development info");
        _logger.LogInformation("This is an INFORMATION message (cyan) - normal operation");
        _logger.LogWarning("This is a WARNING message (gold) - attention needed!");
        _logger.LogError("This is an ERROR message (orange-red) - something went wrong");

        _logger.LogInformation("Emitted test logs at multiple levels to Godot sink");

        // Button is already connected via .tscn signal connection
        // No need to wire it up in code (would cause double-firing)

        _logger.LogInformation("DI Bootstrap Test Scene Ready");
        _logger.LogInformation("Tip: Press F12 to toggle Debug Console (global autoload)");
    }

    private void OnTestButtonPressed()
    {
        _clickCount++;
        _logger?.LogInformation("Test Button Clicked (click #{ClickCount})", _clickCount);

        // Test service resolution on each click
        var result = ServiceLocator.GetService<ITestService>();

        if (result.IsSuccess)
        {
            var message = result.Value.GetTestMessage();
            _logger?.LogInformation("Click #{ClickCount}: {Message}", _clickCount, message);
            UpdateStatus($"Last test: SUCCESS ({_clickCount} clicks)", Colors.Cyan);
        }
        else
        {
            _logger?.LogError("Click #{ClickCount} FAILED: {Error}", _clickCount, result.Error);
            UpdateStatus($"Last test: FAILED - {result.Error}", Colors.Red);
        }
    }

    private void UpdateStatus(string message, Color color)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            _statusLabel.Modulate = color;
        }
    }
}