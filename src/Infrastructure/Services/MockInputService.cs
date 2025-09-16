using LanguageExt;
using Microsoft.Extensions.Logging;
using Darklands.Application.Services;
using Darklands.Domain.Grid;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Darklands.Application.Infrastructure.Services;

/// <summary>
/// Mock implementation of IInputService for testing purposes.
/// Provides controllable input state and event simulation for unit tests.
/// Enables testing input-dependent behavior without actual user interaction.
/// </summary>
public sealed class MockInputService : IInputService
{
    private readonly ILogger<MockInputService>? _logger;
    private readonly Subject<InputEvent> _inputEventSubject;
    private readonly Dictionary<InputAction, bool> _actionStates;
    private readonly Dictionary<InputAction, bool> _justPressedStates;
    private readonly Dictionary<InputAction, bool> _justReleasedStates;

    // Configurable state
    public Position MousePosition { get; set; } = Position.Zero;
    public Position WorldMousePosition { get; set; } = Position.Zero;

    public MockInputService(ILogger<MockInputService>? logger = null)
    {
        _logger = logger;
        _inputEventSubject = new Subject<InputEvent>();
        _actionStates = new Dictionary<InputAction, bool>();
        _justPressedStates = new Dictionary<InputAction, bool>();
        _justReleasedStates = new Dictionary<InputAction, bool>();

        // Initialize all actions to not pressed
        foreach (var action in Enum.GetValues<InputAction>())
        {
            _actionStates[action] = false;
            _justPressedStates[action] = false;
            _justReleasedStates[action] = false;
        }
    }

    public IObservable<InputEvent> InputEvents => _inputEventSubject.AsObservable();

    public bool IsActionPressed(InputAction action)
    {
        return _actionStates.GetValueOrDefault(action, false);
    }

    public bool IsActionJustPressed(InputAction action)
    {
        return _justPressedStates.GetValueOrDefault(action, false);
    }

    public bool IsActionJustReleased(InputAction action)
    {
        return _justReleasedStates.GetValueOrDefault(action, false);
    }

    public Position GetMousePosition()
    {
        return MousePosition;
    }

    public Position GetWorldMousePosition()
    {
        return WorldMousePosition;
    }

    /// <summary>
    /// Simulates pressing an action. Sets pressed state and just-pressed for one update cycle.
    /// </summary>
    public void SimulatePressAction(InputAction action, ulong timestamp = 0)
    {
        var wasPressed = _actionStates.GetValueOrDefault(action, false);

        _actionStates[action] = true;
        _justPressedStates[action] = !wasPressed; // Only just-pressed if it wasn't already pressed
        _justReleasedStates[action] = false;

        // Emit input event
        var inputEvent = new KeyInputEvent
        {
            Action = action,
            IsPressed = true,
            Timestamp = timestamp,
            Handled = false
        };

        _inputEventSubject.OnNext(inputEvent);
        _logger?.LogDebug("Mock simulated press for {Action}", action);
    }

    /// <summary>
    /// Simulates releasing an action. Clears pressed state and sets just-released for one update cycle.
    /// </summary>
    public void SimulateReleaseAction(InputAction action, ulong timestamp = 0)
    {
        var wasPressed = _actionStates.GetValueOrDefault(action, false);

        _actionStates[action] = false;
        _justPressedStates[action] = false;
        _justReleasedStates[action] = wasPressed; // Only just-released if it was pressed

        // Emit input event
        var inputEvent = new KeyInputEvent
        {
            Action = action,
            IsPressed = false,
            Timestamp = timestamp,
            Handled = false
        };

        _inputEventSubject.OnNext(inputEvent);
        _logger?.LogDebug("Mock simulated release for {Action}", action);
    }

    /// <summary>
    /// Simulates a quick press and release of an action.
    /// Useful for testing single-press actions like menu toggles.
    /// </summary>
    public void SimulateActionTap(InputAction action, ulong timestamp = 0)
    {
        SimulatePressAction(action, timestamp);
        // The release will be handled in the next update cycle
        Task.Run(async () =>
        {
            await Task.Delay(16); // Simulate one frame delay
            SimulateReleaseAction(action, timestamp + 1);
        });
    }

    /// <summary>
    /// Simulates a mouse button press at the current mouse position.
    /// </summary>
    public void SimulateMousePress(MouseButton button, Position? position = null, ulong timestamp = 0)
    {
        var mousePos = position ?? MousePosition;
        if (position.HasValue)
        {
            MousePosition = position.Value;
        }

        var mouseEvent = new MouseInputEvent
        {
            Position = mousePos,
            Button = button,
            IsPressed = true,
            Timestamp = timestamp,
            Handled = false
        };

        _inputEventSubject.OnNext(mouseEvent);
        _logger?.LogDebug("Mock simulated mouse press {Button} at {Position}", button, mousePos);
    }

    /// <summary>
    /// Simulates a mouse button release.
    /// </summary>
    public void SimulateMouseRelease(MouseButton button, Position? position = null, ulong timestamp = 0)
    {
        var mousePos = position ?? MousePosition;
        if (position.HasValue)
        {
            MousePosition = position.Value;
        }

        var mouseEvent = new MouseInputEvent
        {
            Position = mousePos,
            Button = button,
            IsPressed = false,
            Timestamp = timestamp,
            Handled = false
        };

        _inputEventSubject.OnNext(mouseEvent);
        _logger?.LogDebug("Mock simulated mouse release {Button} at {Position}", button, mousePos);
    }

    /// <summary>
    /// Simulates a mouse click (press followed by release).
    /// </summary>
    public void SimulateMouseClick(MouseButton button, Position? position = null, ulong timestamp = 0)
    {
        SimulateMousePress(button, position, timestamp);
        Task.Run(async () =>
        {
            await Task.Delay(16); // Simulate one frame delay
            SimulateMouseRelease(button, position, timestamp + 1);
        });
    }

    /// <summary>
    /// Updates the mock input service. Should be called each frame to clear just-pressed/released states.
    /// This simulates how real input systems work.
    /// </summary>
    public void Update()
    {
        // Clear just-pressed and just-released states after one frame
        foreach (var action in Enum.GetValues<InputAction>())
        {
            _justPressedStates[action] = false;
            _justReleasedStates[action] = false;
        }
    }

    /// <summary>
    /// Resets all input states to default (nothing pressed).
    /// </summary>
    public void Reset()
    {
        foreach (var action in Enum.GetValues<InputAction>())
        {
            _actionStates[action] = false;
            _justPressedStates[action] = false;
            _justReleasedStates[action] = false;
        }

        MousePosition = Position.Zero;
        WorldMousePosition = Position.Zero;

        _logger?.LogDebug("Mock input service reset to default state");
    }

    /// <summary>
    /// Gets the current state of all actions for debugging/testing.
    /// </summary>
    public IReadOnlyDictionary<InputAction, bool> GetActionStates()
    {
        return new Dictionary<InputAction, bool>(_actionStates);
    }

    /// <summary>
    /// Disposes the mock input service and completes the input event stream.
    /// </summary>
    public void Dispose()
    {
        _inputEventSubject?.OnCompleted();
        _inputEventSubject?.Dispose();
    }
}
