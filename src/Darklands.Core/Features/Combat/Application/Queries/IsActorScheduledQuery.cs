using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Combat.Application.Queries;

/// <summary>
/// Query to check if an actor is currently scheduled in the turn queue.
/// Used by EnemyDetectionEventHandler to prevent duplicate scheduling (reinforcements).
/// </summary>
/// <param name="ActorId">Actor to check</param>
public record IsActorScheduledQuery(ActorId ActorId) : IRequest<Result<bool>>;
