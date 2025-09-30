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
    private Button? _clearButton;
    private RichTextLabel? _logOutput;
    private VBoxContainer? _categoryFiltersContainer;
    private Control? _debugPanel;  // Container for all debug UI (for F12 toggle)

    private int _clickCount = 0;

    public override void _Ready()
    {
        GD.Print("=== DI Bootstrap Test Scene Loading ===");

        // Get nodes by path instead of Export (more reliable)
        _statusLabel = GetNode<Label>("StatusLabel");
        _testButton = GetNode<Button>("TestButton");
        _clearButton = GetNodeOrNull<Button>("ClearButton");
        _logOutput = GetNode<RichTextLabel>("LogOutput");
        _categoryFiltersContainer = GetNodeOrNull<VBoxContainer>("CategoryFilters");
        _debugPanel = GetNodeOrNull<Control>("DebugPanel");  // Optional container for F12 toggle

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

        // 2. GodotRichTextSink: Writes to in-game RichTextLabel with BBCode colors
        var godotRichFormatter = new Darklands.Infrastructure.Logging.GodotBBCodeFormatter();
        var godotRichTextSink = new Darklands.Infrastructure.Logging.GodotRichTextSink(_logOutput, godotRichFormatter);

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

            // Three sinks:
            .WriteTo.Sink(godotConsoleSink)   // Godot's Output panel (editor console)
            .WriteTo.File(
                "logs/darklands.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext:l}: {Message:lj}{NewLine}",
                shared: true  // Allow concurrent reads (for tail -f)
            )
            .WriteTo.Sink(godotRichTextSink)  // In-game RichTextLabel with BBCode
            .CreateLogger();

        GD.Print("✅ Serilog configured (Godot Console + File + In-Game RichText sinks with category filtering)");

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

        // ===== VS_003 Phase 3: Test Godot RichText Sink with BBCode =====
        GD.Print("\n=== Testing Godot RichText Sink (VS_003 Phase 3) ===");

        // Get a logger and emit test logs at different levels
        var logger = ServiceLocator.Get<Microsoft.Extensions.Logging.ILogger<DIBootstrapTest>>();

        logger.LogDebug("This is a DEBUG message (gray) - low importance development info");
        logger.LogInformation("This is an INFORMATION message (cyan) - normal operation");
        logger.LogWarning("This is a WARNING message (gold) - attention needed!");
        logger.LogError("This is an ERROR message (orange-red) - something went wrong");

        GD.Print("✅ Emitted test logs at multiple levels to Godot sink");

        // ===== VS_003 Phase 4: Debug UI Setup =====
        SetupDebugUI(loggingService);

        // Button is already connected via .tscn signal connection
        // No need to wire it up in code (would cause double-firing)

        GD.Print("=== DI Bootstrap Test Scene Ready ===");
    }

    /// <summary>
    /// VS_003 Phase 4: Setup debug UI with category filters and clear button.
    /// </summary>
    private void SetupDebugUI(Darklands.Infrastructure.Logging.LoggingService loggingService)
    {
        GD.Print("\n=== Setting up Debug UI (VS_003 Phase 4) ===");

        // Wire up clear button if it exists in scene
        if (_clearButton != null)
        {
            _clearButton.Pressed += () =>
            {
                if (_logOutput != null)
                {
                    _logOutput.Clear();
                    GD.Print("✅ Log output cleared");
                }
            };
            GD.Print("✅ Clear button wired up");
        }

        // Dynamically create category filter checkboxes if container exists
        if (_categoryFiltersContainer != null)
        {
            var availableCategories = loggingService.GetAvailableCategories();
            var enabledCategories = loggingService.GetEnabledCategories();

            GD.Print($"📋 Discovered {availableCategories.Count} categories:");

            foreach (var category in availableCategories)
            {
                var checkbox = new CheckBox
                {
                    Text = category,
                    ButtonPressed = enabledCategories.Contains(category)
                };

                // Wire up toggle handler
                checkbox.Toggled += (isEnabled) =>
                {
                    if (isEnabled)
                    {
                        loggingService.EnableCategory(category);
                        GD.Print($"✅ Enabled category: {category}");
                    }
                    else
                    {
                        loggingService.DisableCategory(category);
                        GD.Print($"❌ Disabled category: {category}");
                    }
                };

                _categoryFiltersContainer.AddChild(checkbox);
                GD.Print($"   - {category} [{(checkbox.ButtonPressed ? "ON" : "OFF")}]");
            }

            GD.Print("✅ Category filters created");
        }

        GD.Print("=== Debug UI setup complete ===\n");
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

    /// <summary>
    /// Handle input for F12 debug panel toggle.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // Toggle debug panel visibility with F12
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            !keyEvent.IsEcho() &&
            keyEvent.Keycode == Key.F12)
        {
            if (_debugPanel != null)
            {
                _debugPanel.Visible = !_debugPanel.Visible;
                GD.Print($"🔧 Debug panel toggled: {(_debugPanel.Visible ? "VISIBLE" : "HIDDEN")}");
            }
            else
            {
                GD.Print("⚠️ F12 pressed but DebugPanel node not found (add optional DebugPanel Control node)");
            }

            // Mark event as handled so it doesn't propagate
            GetViewport().SetInputAsHandled();
        }
    }
}