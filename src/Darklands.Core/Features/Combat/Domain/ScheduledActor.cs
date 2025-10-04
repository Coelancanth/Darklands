using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Combat.Domain;

/// <summary>
/// Represents an actor scheduled in the turn queue with their next action time.
/// Immutable record - use with-expressions to update time.
/// </summary>
/// <remarks>
/// DESIGN: Simple data holder for turn queue entries.
/// TurnQueue aggregate owns scheduling logic, ScheduledActor is just data.
///
/// WHY: Separates "who acts when" (data) from "how queue operates" (TurnQueue logic).
/// </remarks>
/// <param name="ActorId">The actor scheduled for action</param>
/// <param name="NextActionTime">When this actor will act next</param>
/// <param name="IsPlayer">True if this is the player character (for tie-breaking)</param>
public readonly record struct ScheduledActor(
    ActorId ActorId,
    TimeUnits NextActionTime,
    bool IsPlayer = false);
