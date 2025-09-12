using Darklands.SharedKernel.Contracts;
using Darklands.SharedKernel.Domain;

namespace Darklands.Tactical.Contracts;

/// <summary>
/// Contract event published when an attack is executed between actors.
/// Used for cross-context communication and parallel operation validation.
/// Uses deterministic integer types to comply with ADR-004.
/// Version 1 of this contract.
/// </summary>
public sealed record AttackExecutedEvent(
    EntityId AttackerId,        // SharedKernel EntityId for cross-context compatibility  
    EntityId TargetId,          // Target of the attack
    string ActionName,          // Name of the combat action (e.g., "Melee Attack")
    int Damage,                 // Amount of damage dealt
    int TimeCost,               // Time units consumed by the action
    bool TargetDefeated,        // Whether the target was defeated by this attack
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
    public static AttackExecutedEvent Create(
        EntityId attackerId,
        EntityId targetId,
        string actionName,
        int damage,
        int timeCost,
        bool targetDefeated) => 
        new(attackerId, targetId, actionName, damage, timeCost, targetDefeated,
            Guid.NewGuid(), DateTime.UtcNow, CurrentVersion);
}