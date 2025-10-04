using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Combat.Domain.Events;

/// <summary>
/// Domain event raised when the turn queue changes (actor added/removed).
/// Presentation layer subscribes for UI updates (turn order display).
/// Combat system subscribes to cancel auto-movement when combat starts.
/// </summary>
/// <param name="ActorId">Actor that was scheduled or removed</param>
/// <param name="ChangeType">Type of change (scheduled or removed)</param>
/// <param name="IsInCombat">Current combat state after the change</param>
/// <param name="QueueSize">Number of actors in queue after the change</param>
/// <remarks>
/// Per ADR-004 Event Rules:
/// - Rule 2: Past tense (ChangedEvent, not ChangeEvent) - describes a fact
/// - Rule 3: Terminal subscriber (Presentation updates UI, CombatModeHandler cancels movement)
/// - Rule 4: Depth = 1 (no cascading events)
///
/// KEY USE CASE: When queue size transitions from 1 → 2 (exploration → combat),
/// IsInCombat changes from false → true, triggering movement cancellation.
/// </remarks>
public record TurnQueueChangedEvent(
    ActorId ActorId,
    TurnQueueChangeType ChangeType,
    bool IsInCombat,
    int QueueSize) : INotification;
