using LanguageExt;
using Darklands.Domain.Grid;
using System;

namespace Darklands.Application.Services;

/// <summary>
/// Abstraction for input handling functionality.
/// Enables input remapping, replay systems, platform differences, and testing.
/// Per ADR-006: This service qualifies for abstraction due to testing, remapping, and platform variance.
/// </summary>
public interface IInputService
{
    /// <summary>
    /// Checks if the specified input action is currently pressed.
    /// </summary>
    /// <param name="action">The input action to check</param>
    /// <returns>True if the action is currently pressed</returns>
    bool IsActionPressed(InputAction action);

    /// <summary>
    /// Checks if the specified input action was just pressed this frame.
    /// </summary>
    /// <param name="action">The input action to check</param>
    /// <returns>True if the action was just pressed</returns>
    bool IsActionJustPressed(InputAction action);

    /// <summary>
    /// Checks if the specified input action was just released this frame.
    /// </summary>
    /// <param name="action">The input action to check</param>
    /// <returns>True if the action was just released</returns>
    bool IsActionJustReleased(InputAction action);

    /// <summary>
    /// Gets the current mouse position in screen coordinates.
    /// Returns domain Position type to avoid Godot Vector2 leakage.
    /// </summary>
    /// <returns>Current mouse position</returns>
    Position GetMousePosition();

    /// <summary>
    /// Gets the mouse position relative to the game world/grid.
    /// Useful for translating screen coordinates to grid coordinates.
    /// </summary>
    /// <returns>Mouse position in world coordinates</returns>
    Position GetWorldMousePosition();

    /// <summary>
    /// Observable stream of input events for reactive programming patterns.
    /// Enables input recording, replay, and complex input handling.
    /// </summary>
    IObservable<InputEvent> InputEvents { get; }
}

/// <summary>
/// Strongly-typed input actions for the game.
/// Maps to Godot input map actions but keeps domain layer clean.
/// </summary>
public enum InputAction
{
    // Movement
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,

    // Combat
    Attack,
    Defend,
    SpecialAction,

    // UI
    Confirm,
    Cancel,
    Menu,
    Inventory,

    // Camera/View
    ZoomIn,
    ZoomOut,
    CameraReset,

    // Development/Debug
    ToggleDebugOverlay,
    QuickSave,
    QuickLoad
}

/// <summary>
/// Domain representation of input events.
/// Avoids Godot's InputEvent class in the domain layer.
/// </summary>
public abstract record InputEvent
{
    /// <summary>
    /// Timestamp when the event occurred (game time, not wall clock).
    /// </summary>
    public abstract ulong Timestamp { get; init; }

    /// <summary>
    /// Whether this event has been handled by some system.
    /// </summary>
    public bool Handled { get; init; }
}

/// <summary>
/// Domain representation of a key press/release event.
/// </summary>
public sealed record KeyInputEvent : InputEvent
{
    public required InputAction Action { get; init; }
    public required bool IsPressed { get; init; }
    public override required ulong Timestamp { get; init; }
}

/// <summary>
/// Domain representation of a mouse event.
/// </summary>
public sealed record MouseInputEvent : InputEvent
{
    public required Position Position { get; init; }
    public required MouseButton Button { get; init; }
    public required bool IsPressed { get; init; }
    public override required ulong Timestamp { get; init; }
}

/// <summary>
/// Mouse button enumeration for domain events.
/// </summary>
public enum MouseButton
{
    Left,
    Right,
    Middle,
    WheelUp,
    WheelDown
}
