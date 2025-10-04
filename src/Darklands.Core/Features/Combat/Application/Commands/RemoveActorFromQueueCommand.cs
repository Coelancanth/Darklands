using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Combat.Application.Commands;

/// <summary>
/// Command to remove an actor from the turn queue.
/// Used when actors are defeated, flee combat, or become incapacitated.
/// </summary>
/// <param name="ActorId">Actor to remove from queue</param>
public record RemoveActorFromQueueCommand(ActorId ActorId) : IRequest<Result>;
