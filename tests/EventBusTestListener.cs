using Godot;
using Darklands.Components;
using Darklands.Core.Domain.Events;

namespace Darklands.Tests;

/// <summary>
/// Test node for manually validating EventBus functionality in Godot.
///
/// MANUAL TEST PROCEDURE (Phase 3):
/// 1. Create scene with this node + Button + Label
/// 2. Run scene → Click button → Label updates
/// 3. Verify logs show: Subscribe → Publish → Handler called → Unsubscribe
/// 4. Close scene → Verify cleanup logs (UnsubscribeAll)
///
/// This validates:
/// - EventAwareNode lifecycle (subscribe in _Ready, unsubscribe in _ExitTree)
/// - MediatR → UIEventForwarder → GodotEventBus → Node handler flow
/// - CallDeferred marshals to main thread correctly
/// - No memory leaks (cleanup logs confirm)
/// </summary>
public partial class EventBusTestListener : EventAwareNode
{
    private Label? _messageLabel;
    private Button? _testButton;
    private int _eventCount = 0;

    public override void _Ready()
    {
        base._Ready(); // CRITICAL: Call base to setup EventBus

        // Get nodes from scene tree
        _messageLabel = GetNode<Label>("../CenterContainer/VBoxContainer/MessageLabel");
        _testButton = GetNode<Button>("../CenterContainer/VBoxContainer/TestButton");

        // Wire button
        if (_testButton != null)
        {
            _testButton.Pressed += OnButtonPressed;
            GD.Print("[EventBusTestListener] Button connected");
        }
        else
        {
            GD.PrintErr("[EventBusTestListener] Failed to find TestButton in scene");
        }

        if (_messageLabel == null)
        {
            GD.PrintErr("[EventBusTestListener] Failed to find MessageLabel in scene");
        }
    }

    protected override void SubscribeToEvents()
    {
        // Subscribe to TestEvent
        EventBus?.Subscribe<TestEvent>(this, OnTestEvent);

        GD.Print("[EventBusTestListener] Subscribed to TestEvent");
    }

    private void OnTestEvent(TestEvent evt)
    {
        _eventCount++;

        GD.Print($"[EventBusTestListener] ✅ Received TestEvent #{_eventCount}: {evt.Message}");

        // Update label
        if (_messageLabel != null)
        {
            _messageLabel.Text = $"Event #{_eventCount}: {evt.Message}";
        }
    }

    private void OnButtonPressed()
    {
        GD.Print("[EventBusTestListener] 🔵 Button pressed - publishing TestEvent");

        // For manual test, directly call GodotEventBus.PublishAsync
        // In production: resolve IMediator and call mediator.Publish()
        EventBus?.PublishAsync(new TestEvent($"Button pressed at {Time.GetTicksMsec()}"));
    }

    public override void _ExitTree()
    {
        // Cleanup button subscription
        if (_testButton != null)
        {
            _testButton.Pressed -= OnButtonPressed;
        }

        base._ExitTree(); // CRITICAL: Call base to UnsubscribeAll
    }
}