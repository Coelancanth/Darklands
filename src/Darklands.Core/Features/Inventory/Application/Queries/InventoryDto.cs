using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Domain;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Data Transfer Object for inventory state.
/// Prevents presentation layer from directly accessing domain entities.
/// Changes to Inventory entity don't break UI code.
/// </summary>
/// <param name="InventoryId">Unique inventory identifier</param>
/// <param name="ActorId">Owner of this inventory</param>
/// <param name="Capacity">Maximum number of items</param>
/// <param name="Count">Current number of items</param>
/// <param name="IsFull">True if at capacity</param>
/// <param name="Items">Read-only list of item IDs (VS_008 backward compat)</param>
/// <param name="GridWidth">Grid width in cells (VS_018 spatial)</param>
/// <param name="GridHeight">Grid height in cells (VS_018 spatial)</param>
/// <param name="ContainerType">Container type restrictions (VS_018 spatial)</param>
/// <param name="ItemPlacements">Dictionary of ItemId → GridPosition mappings (VS_018 spatial)</param>
/// <remarks>
/// VS_018: Added spatial fields as required (not optional) to force compile-time updates.
/// Old slot-based UI uses Items list, new spatial UI uses GridWidth/GridHeight/ItemPlacements.
/// </remarks>
public sealed record InventoryDto(
    InventoryId InventoryId,
    ActorId ActorId,
    int Capacity,
    int Count,
    bool IsFull,
    IReadOnlyList<ItemId> Items,
    int GridWidth,
    int GridHeight,
    ContainerType ContainerType,
    IReadOnlyDictionary<ItemId, GridPosition> ItemPlacements);
