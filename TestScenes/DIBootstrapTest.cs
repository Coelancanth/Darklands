using Godot;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Infrastructure.DependencyInjection;

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

        // Initialize DI container (this is normally done in Main scene root)
        var initResult = GameStrapper.Initialize();

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