using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Item.Domain;

/// <summary>
/// Represents an item that can exist in the game world (inventory, equipment, loot).
/// </summary>
/// <remarks>
/// ARCHITECTURE (ADR-002): Item stores primitive atlas coordinates, NOT Godot types.
/// Infrastructure layer reads TileSet metadata and calls Create() with primitives.
///
/// DATA-FIRST DESIGN: TileSet custom data layers define the contract (Phase 0),
/// this entity implements TO that contract. Properties mirror TileSet metadata schema.
/// </remarks>
public sealed class Item
{
    /// <summary>
    /// Unique identifier for this item instance.
    /// </summary>
    public ItemId Id { get; private init; }

    /// <summary>
    /// Atlas X coordinate for sprite lookup (primitive, no Godot dependency).
    /// </summary>
    public int AtlasX { get; private init; }

    /// <summary>
    /// Atlas Y coordinate for sprite lookup (primitive, no Godot dependency).
    /// </summary>
    public int AtlasY { get; private init; }

    /// <summary>
    /// Item name from TileSet custom_data_0 (item_name).
    /// </summary>
    public string Name { get; private init; }

    /// <summary>
    /// Item type from TileSet custom_data_1 (item_type).
    /// Examples: "weapon", "item", "UI"
    /// </summary>
    public string Type { get; private init; }

    /// <summary>
    /// Width in grid cells from TileSet size_in_atlas.x.
    /// </summary>
    public int Width { get; private init; }

    /// <summary>
    /// Height in grid cells from TileSet size_in_atlas.y.
    /// </summary>
    public int Height { get; private init; }

    /// <summary>
    /// Maximum stack size from TileSet custom_data_2 (max_stack_size).
    /// 0 or 1 = not stackable, >1 = stackable.
    /// </summary>
    public int MaxStackSize { get; private init; }

    /// <summary>
    /// Computed property: true if MaxStackSize > 1, false otherwise.
    /// </summary>
    public bool IsStackable => MaxStackSize > 1;

    private Item(
        ItemId id,
        int atlasX,
        int atlasY,
        string name,
        string type,
        int width,
        int height,
        int maxStackSize)
    {
        Id = id;
        AtlasX = atlasX;
        AtlasY = atlasY;
        Name = name;
        Type = type;
        Width = width;
        Height = height;
        MaxStackSize = maxStackSize;
    }

    /// <summary>
    /// Creates a new Item from primitive values (TileSet metadata).
    /// </summary>
    /// <param name="id">Unique item instance identifier</param>
    /// <param name="atlasX">Atlas X coordinate (non-negative)</param>
    /// <param name="atlasY">Atlas Y coordinate (non-negative)</param>
    /// <param name="name">Item name (non-empty)</param>
    /// <param name="type">Item type (non-empty)</param>
    /// <param name="width">Width in grid cells (positive)</param>
    /// <param name="height">Height in grid cells (positive)</param>
    /// <param name="maxStackSize">Maximum stack size (non-negative)</param>
    /// <returns>Result containing Item or validation error</returns>
    public static Result<Item> Create(
        ItemId id,
        int atlasX,
        int atlasY,
        string name,
        string type,
        int width,
        int height,
        int maxStackSize)
    {
        // BUSINESS RULE: Atlas coordinates must be non-negative
        if (atlasX < 0)
            return Result.Failure<Item>("Atlas X coordinate must be non-negative");

        if (atlasY < 0)
            return Result.Failure<Item>("Atlas Y coordinate must be non-negative");

        // BUSINESS RULE: Name is required
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Item>("Item name cannot be empty");

        // BUSINESS RULE: Type is required
        if (string.IsNullOrWhiteSpace(type))
            return Result.Failure<Item>("Item type cannot be empty");

        // BUSINESS RULE: Width and height must be positive (items occupy space)
        if (width <= 0)
            return Result.Failure<Item>("Width must be positive");

        if (height <= 0)
            return Result.Failure<Item>("Height must be positive");

        // BUSINESS RULE: Max stack size must be non-negative
        if (maxStackSize < 0)
            return Result.Failure<Item>("Max stack size must be non-negative");

        return Result.Success(new Item(
            id,
            atlasX,
            atlasY,
            name,
            type,
            width,
            height,
            maxStackSize));
    }
}
