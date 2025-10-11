using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to retrieve inventory state by ID.
/// Returns DTO to prevent presentation layer from directly accessing domain entities.
/// </summary>
/// <param name="InventoryId">Inventory to retrieve</param>
/// <remarks>
/// TD_019: Changed from ActorId to InventoryId parameter (breaking change).
/// Migration: Use GetByOwnerAsync in repository to find actor's inventory ID first.
/// </remarks>
public sealed record GetInventoryQuery(InventoryId InventoryId) : IRequest<Result<InventoryDto>>;
