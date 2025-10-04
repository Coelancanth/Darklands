using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Domain;

namespace Darklands.Core.Features.Combat.Application.Queries;

/// <summary>
/// DTO representing the current turn queue state.
/// </summary>
public record TurnQueueStateDto(
    bool IsInCombat,
    int QueueSize,
    List<ScheduledActorDto> ScheduledActors);

/// <summary>
/// DTO for a single scheduled actor in the turn queue.
/// </summary>
public record ScheduledActorDto(
    ActorId ActorId,
    TimeUnits NextActionTime,
    bool IsPlayer);
