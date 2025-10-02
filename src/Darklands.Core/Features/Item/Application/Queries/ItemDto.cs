using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Item.Application.Queries;

/// <summary>
/// Data Transfer Object for item catalog data.
/// Prevents presentation layer from directly accessing domain entities.
/// Changes to Item entity don't break UI code.
/// </summary>
/// <remarks>
/// DTO mirrors TileSet metadata contract (Phase 0):
/// - Atlas coordinates for sprite rendering
/// - Name, Type from custom_data_0 and custom_data_1
/// - Width/Height from size_in_atlas
/// - MaxStackSize from custom_data_2
/// - IsStackable computed property
///
/// PRESENTATION USE CASES:
/// - Item showcase UI (display all items with sprites)
/// - Inventory UI (render item sprites at atlas coords)
/// - Loot tables (filter by type, check stackability)
/// </remarks>
/// <param name="Id">Unique item instance identifier</param>
/// <param name="AtlasX">Atlas X coordinate for sprite lookup</param>
/// <param name="AtlasY">Atlas Y coordinate for sprite lookup</param>
/// <param name="Name">Item name from TileSet custom_data_0</param>
/// <param name="Type">Item type from TileSet custom_data_1 (weapon, item, UI)</param>
/// <param name="Width">Width in grid cells from size_in_atlas.x</param>
/// <param name="Height">Height in grid cells from size_in_atlas.y</param>
/// <param name="MaxStackSize">Max stack size from custom_data_2 (0/1 = non-stackable, >1 = stackable)</param>
/// <param name="IsStackable">Computed: true if MaxStackSize > 1</param>
public sealed record ItemDto(
    ItemId Id,
    int AtlasX,
    int AtlasY,
    string Name,
    string Type,
    int Width,
    int Height,
    int MaxStackSize,
    bool IsStackable);
