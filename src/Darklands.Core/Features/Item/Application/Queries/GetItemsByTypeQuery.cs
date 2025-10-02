using CSharpFunctionalExtensions;
using MediatR;

namespace Darklands.Core.Features.Item.Application.Queries;

/// <summary>
/// Query to retrieve all items of a specific type.
/// Used for filtered displays (e.g., "show all weapons", "show all consumables").
/// </summary>
/// <param name="Type">Item type from TileSet custom_data_1 (e.g., "weapon", "item", "UI")</param>
public record GetItemsByTypeQuery(string Type) : IRequest<Result<List<ItemDto>>>;
