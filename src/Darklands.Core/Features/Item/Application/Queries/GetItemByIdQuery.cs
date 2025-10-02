using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Item.Application.Queries;

/// <summary>
/// Query to retrieve a specific item by its ID.
/// Used when inventory/equipment systems need item details for display.
/// </summary>
/// <param name="ItemId">Unique identifier of the item to retrieve</param>
public record GetItemByIdQuery(ItemId ItemId) : IRequest<Result<ItemDto>>;
