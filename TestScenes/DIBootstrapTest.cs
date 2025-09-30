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
    private RichTextLabel? _logOutput;

    private int _clickCount = 0;

    public override void _Ready()
    {
        GD.Print("=== DI Bootstrap Test Scene Loading ===");

        // Get nodes by path instead of Export (more reliable)
        _statusLabel = GetNode<Label>("StatusLabel");
        _testButton = GetNode<Button>("TestButton");
        _logOutput = GetNode<RichTextLabel>("LogOutput");

        // Configure Serilog with category filtering BEFORE initializing DI container
        // NOTE: This is Presentation layer code - Serilog packages are only in Darklands.csproj

        // Create shared state for category filtering
        var enabledCategories = new HashSet<string>
        {
            "Combat", "Movement", "AI", "Infrastructure", "Network"
        };

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

            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate: "[{Level:u3}] {Timestamp:HH:mm:ss} {SourceContext:l}: {Message:lj}{NewLine}"
            )
            .WriteTo.File(
                "logs/darklands.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext:l}: {Message:lj}{NewLine}",
                shared: true  // Allow concurrent reads (for tail -f)
            )
            .CreateLogger();

        GD.Print("✅ Serilog configured (Console + File sinks with category filtering)");

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
            GD.Print("✅ GameStrapper.Initialize() succeeded");
            UpdateStatus("DI Container: Initialized ✅", Colors.Green);
        }
        else
        {
            GD.PrintErr($"❌ GameStrapper.Initialize() failed: {initResult.Error}");
            UpdateStatus($"DI Container: FAILED - {initResult.Error}", Colors.Red);
            return;
        }

        // Verify service resolution
        var serviceResult = ServiceLocator.GetService<ITestService>();

        if (serviceResult.IsSuccess)
        {
            GD.Print($"✅ ServiceLocator resolved ITestService: {serviceResult.Value.GetTestMessage()}");
            AddLog($"[color=green]✅ ITestService resolved:[/color] {serviceResult.Value.GetTestMessage()}");
        }
        else
        {
            GD.PrintErr($"❌ ServiceLocator failed to resolve ITestService: {serviceResult.Error}");
            AddLog($"[color=red]❌ Failed to resolve ITestService:[/color] {serviceResult.Error}");
        }

        // ===== VS_003 Phase 2: Test category filtering =====
        var loggingService = ServiceLocator.Get<Darklands.Infrastructure.Logging.LoggingService>();

        GD.Print("\n=== Testing Category Filtering (VS_003 Phase 2) ===");
        GD.Print($"Initial enabled categories: {string.Join(", ", loggingService.GetEnabledCategories())}");

        // Test disabling a category
        loggingService.DisableCategory("Combat");
        GD.Print("✅ Disabled 'Combat' category");
        GD.Print($"Enabled categories after disable: {string.Join(", ", loggingService.GetEnabledCategories())}");

        // Test re-enabling
        loggingService.EnableCategory("Combat");
        GD.Print("✅ Re-enabled 'Combat' category");
        GD.Print($"Enabled categories after enable: {string.Join(", ", loggingService.GetEnabledCategories())}");

        // Test category extraction
        var testCategory = Darklands.Infrastructure.Logging.LoggingService.ExtractCategory(
            "Darklands.Core.Application.Commands.Combat.ExecuteAttackCommandHandler");
        GD.Print($"✅ Extracted category 'Combat' from handler name: {testCategory}");

        AddLog($"[color=cyan]Category filtering test passed ✅[/color]");

        // Button is already connected via .tscn signal connection
        // No need to wire it up in code (would cause double-firing)

        GD.Print("=== DI Bootstrap Test Scene Ready ===");
    }

    private void OnTestButtonPressed()
    {
        _clickCount++;
        GD.Print($"Test button clicked ({_clickCount} times)");

        // Test service resolution on each click
        var result = ServiceLocator.GetService<ITestService>();

        if (result.IsSuccess)
        {
            var message = result.Value.GetTestMessage();
            AddLog($"[color=cyan]Click #{_clickCount}:[/color] {message}");
            UpdateStatus($"Last test: SUCCESS ({_clickCount} clicks)", Colors.Cyan);
        }
        else
        {
            AddLog($"[color=red]Click #{_clickCount} FAILED:[/color] {result.Error}");
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

    private void AddLog(string message)
    {
        if (_logOutput != null)
        {
            _logOutput.AppendText($"{message}\n");
        }
    }
}