namespace Darklands.Core.Domain.Debug;

/// <summary>
/// Defines categories for filtered debug logging to allow runtime control of log verbosity.
/// Each category corresponds to a major system or feature area that can be independently enabled/disabled.
/// </summary>
public enum LogCategory
{
    /// <summary>
    /// General system messages, startup, shutdown, critical operations.
    /// </summary>
    System,

    /// <summary>
    /// Command execution, MediatR pipeline operations, request/response cycles.
    /// </summary>
    Command,

    /// <summary>
    /// Domain events, event sourcing, notification patterns.
    /// </summary>
    Event,

    /// <summary>
    /// Threading, async operations, task scheduling, parallel execution.
    /// </summary>
    Thread,

    /// <summary>
    /// AI decision making, behavior trees, state machines, targeting.
    /// </summary>
    AI,

    /// <summary>
    /// Performance monitoring, frame times, memory usage, profiling data.
    /// </summary>
    Performance,

    /// <summary>
    /// Network communications, multiplayer sync, connection states.
    /// </summary>
    Network,

    /// <summary>
    /// General developer information, temporary logging, development traces.
    /// </summary>
    Developer,

    /// <summary>
    /// Gameplay events, actor movement, player actions, game state changes.
    /// </summary>
    Gameplay,

    /// <summary>
    /// Vision system, field of view calculations, line of sight, fog of war.
    /// </summary>
    Vision,

    /// <summary>
    /// Pathfinding algorithms, navigation mesh, route calculations.
    /// </summary>
    Pathfinding,

    /// <summary>
    /// Combat calculations, damage resolution, hit chances, turn order.
    /// </summary>
    Combat
}
