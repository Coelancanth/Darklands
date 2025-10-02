using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to retrieve an actor's inventory state.
/// Returns DTO to prevent presentation layer from directly accessing domain entities.
/// </summary>
/// <param name="ActorId">Actor whose inventory to retrieve</param>
public sealed record GetInventoryQuery(ActorId ActorId) : IRequest<Result<InventoryDto>>;
