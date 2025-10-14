using Godot;
using Darklands.Presentation.Infrastructure.Events;
using Darklands.Core.Domain.Events;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Infrastructure.Events;

namespace Darklands.TestScenes;

/// <summary>
/// Test node for manually validating EventBus functionality in Godot.
/// Enhanced with detailed logging and status feedback.
/// </summary>
public partial class EventBusTestListener : EventAwareNode
{
    private Label? _messageLabel;
    private Label? _statusLabel;
    private Button? _testButton;
    private int _eventCount = 0;

    public override void _Ready()
    {
        base._Ready(); // CRITICAL: Call base to setup EventBus

        GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        GD.Print("🧪 EventBusTestListener._Ready() called");
        GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // Get nodes from scene tree
        _messageLabel = GetNodeOrNull<Label>("../CenterContainer/VBoxContainer/MessageLabel");
        _statusLabel = GetNodeOrNull<Label>("../CenterContainer/VBoxContainer/StatusLabel");
        _testButton = GetNodeOrNull<Button>("../CenterContainer/VBoxContainer/TestButton");

        // Check if EventBus was resolved
        if (EventBus == null)
        {
            GD.PrintErr("❌ EventBus is NULL - DI container not initialized!");
            if (_statusLabel != null)
            {
                _statusLabel.Text = "❌ ERROR: EventBus not initialized\nCheck Output panel";
                _statusLabel.AddThemeColorOverride("font_color", new Color(1, 0, 0));
            }
            return;
        }

        GD.Print("✅ EventBus resolved successfully");

        // Wire button
        if (_testButton != null)
        {
            _testButton.Pressed += OnButtonPressed;
            GD.Print("✅ Button connected");
        }
        else
        {
            GD.PrintErr("❌ Failed to find TestButton in scene");
        }

        if (_messageLabel == null)
        {
            GD.PrintErr("❌ Failed to find MessageLabel in scene");
        }

        if (_statusLabel != null)
        {
            _statusLabel.Text = "✅ EventBus initialized\n✅ Button connected\nReady to test!";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0, 1, 0));
        }

        // Test ServiceLocator directly
        var serviceResult = ServiceLocator.GetService<IGodotEventBus>();
        GD.Print($"ServiceLocator.GetService result: {(serviceResult.IsSuccess ? "✅ Success" : $"❌ Failure: {serviceResult.Error}")}");

        GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }

    protected override void SubscribeToEvents()
    {
        GD.Print("🔔 SubscribeToEvents() called");

        // Subscribe to TestEvent
        EventBus?.Subscribe<TestEvent>(this, OnTestEvent);

        GD.Print("✅ Subscribed to TestEvent");
    }

    private void OnTestEvent(TestEvent evt)
    {
        _eventCount++;

        GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        GD.Print($"✅ EVENT RECEIVED #{_eventCount}: {evt.Message}");
        GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // Update label
        if (_messageLabel != null)
        {
            _messageLabel.Text = $"Event #{_eventCount}: {evt.Message}";
        }
    }

    private void OnButtonPressed()
    {
        GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        GD.Print("🔵 Button pressed!");

        if (EventBus == null)
        {
            GD.PrintErr("❌ EventBus is NULL - cannot publish event");
            GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            return;
        }

        GD.Print("📤 Publishing TestEvent...");

        // For manual test, directly call GodotEventBus.PublishAsync
        var testEvent = new TestEvent($"Button pressed at {Time.GetTicksMsec()}");
        EventBus.PublishAsync(testEvent);

        GD.Print($"✅ PublishAsync called with: {testEvent.Message}");
        GD.Print("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }

    public override void _ExitTree()
    {
        GD.Print("🧹 EventBusTestListener._ExitTree() - cleaning up");

        // Cleanup button subscription
        if (_testButton != null)
        {
            _testButton.Pressed -= OnButtonPressed;
        }

        base._ExitTree(); // CRITICAL: Call base to UnsubscribeAll
    }
}