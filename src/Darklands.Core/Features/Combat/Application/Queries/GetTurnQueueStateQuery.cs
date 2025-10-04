using CSharpFunctionalExtensions;
using MediatR;

namespace Darklands.Core.Features.Combat.Application.Queries;

/// <summary>
/// Query to get the current turn queue state (all scheduled actors).
/// Used for UI display and combat end detection.
/// </summary>
public record GetTurnQueueStateQuery() : IRequest<Result<TurnQueueStateDto>>;
