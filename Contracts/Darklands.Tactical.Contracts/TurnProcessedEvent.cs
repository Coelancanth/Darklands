using Darklands.SharedKernel.Contracts;
using Darklands.SharedKernel.Domain;

namespace Darklands.Tactical.Contracts;

/// <summary>
/// Contract event published when a turn is processed in the combat scheduler.
/// Used for cross-context communication and parallel operation validation.
/// Uses deterministic integer types to comply with ADR-004.
/// Version 1 of this contract.
/// </summary>
public sealed record TurnProcessedEvent(
    EntityId ActorId,           // Actor whose turn was processed
    int CurrentTime,            // Current game time in time units
    int NextTurnTime,           // When this actor's next turn will be
    int ActorsRemaining,        // Number of actors still in the scheduler
    Guid Id,                    // Required by IContractEvent
    DateTime OccurredAt,        // Required by IContractEvent
    int Version                 // Required by IContractEvent for contract evolution
) : IContractEvent
{
    /// <summary>
    /// Current version of this contract event.
    /// </summary>
    public const int CurrentVersion = 1;
    
    /// <summary>
    /// Factory method to create event with current timestamp, new ID, and current version.
    /// </summary>
    public static TurnProcessedEvent Create(
        EntityId actorId,
        int currentTime,
        int nextTurnTime,
        int actorsRemaining) => 
        new(actorId, currentTime, nextTurnTime, actorsRemaining,
            Guid.NewGuid(), DateTime.UtcNow, CurrentVersion);
}