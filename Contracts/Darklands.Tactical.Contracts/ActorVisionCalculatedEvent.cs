using Darklands.SharedKernel.Contracts;
using Darklands.SharedKernel.Domain;
using MediatR;

namespace Darklands.Tactical.Contracts;

/// <summary>
/// Contract event published when an actor's vision/FOV is calculated.
/// Used for cross-context communication to Diagnostics context.
/// Uses deterministic integer types to comply with ADR-004.
/// Implements INotification for MediatR integration.
/// </summary>
public sealed record ActorVisionCalculatedEvent(
    EntityId ActorId,           // SharedKernel EntityId for cross-context compatibility
    int TilesVisible,           // Deterministic integer count
    int CalculationTimeMs,      // Integer milliseconds (converted from double)
    int TilesChecked,           // Total tiles checked during calculation
    bool WasFromCache,          // Whether result came from cache
    DateTime OccurredAt,        // Required by IContractEvent
    Guid EventId                // Required by IContractEvent
) : IContractEvent, INotification
{
    /// <summary>
    /// Factory method to create event with current timestamp and new ID.
    /// </summary>
    /// <param name="actorId">Actor the calculation was for</param>
    /// <param name="tilesVisible">Number of tiles made visible</param>
    /// <param name="calculationTimeMs">Time taken in milliseconds (converted to int)</param>
    /// <param name="tilesChecked">Total tiles checked</param>
    /// <param name="wasFromCache">Whether result came from cache</param>
    public static ActorVisionCalculatedEvent Create(
        EntityId actorId, 
        int tilesVisible, 
        int calculationTimeMs, 
        int tilesChecked, 
        bool wasFromCache) => 
        new(actorId, tilesVisible, calculationTimeMs, tilesChecked, wasFromCache, 
            DateTime.UtcNow, Guid.NewGuid());
}