using CSharpFunctionalExtensions;
using MediatR;

namespace Darklands.Core.Features.Item.Application.Queries;

/// <summary>
/// Query to retrieve all items in the catalog.
/// Used by item showcase UI, inventory systems, loot tables.
/// </summary>
/// <remarks>
/// PERFORMANCE: Items are catalog data (loaded once, cached).
/// This query is cheap - no database roundtrips, just in-memory list access.
/// </remarks>
public record GetAllItemsQuery : IRequest<Result<List<ItemDto>>>;
