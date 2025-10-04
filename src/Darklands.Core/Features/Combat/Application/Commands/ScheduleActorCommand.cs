using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Domain;
using MediatR;

namespace Darklands.Core.Features.Combat.Application.Commands;

/// <summary>
/// Command to schedule an actor for action at a specific time.
/// Used when enemies are detected (FOV events) or actors need rescheduling after actions.
/// </summary>
/// <param name="ActorId">Actor to schedule</param>
/// <param name="NextActionTime">When this actor should act</param>
/// <param name="IsPlayer">True if this is the player character (for tie-breaking)</param>
public record ScheduleActorCommand(
    ActorId ActorId,
    TimeUnits NextActionTime,
    bool IsPlayer = false) : IRequest<Result>;
