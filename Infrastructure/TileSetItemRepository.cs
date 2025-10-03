using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using Godot;
using Microsoft.Extensions.Logging;
using ItemEntity = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Infrastructure;

/// <summary>
/// Item repository that auto-discovers items from Godot TileSet resource.
/// </summary>
/// <remarks>
/// ARCHITECTURE (ADR-002 Compliance):
/// - Lives in Presentation layer (Infrastructure folder) because it uses Godot types
/// - Extracts primitives from TileSet metadata
/// - Calls Item.Create() with primitives (Domain stays Godot-free)
///
/// AUTO-DISCOVERY WORKFLOW:
/// 1. Constructor receives TileSet loaded by DI container setup
/// 2. Enumerates all tiles in atlas source 0
/// 3. Reads custom data (item_name, item_type, max_stack_size)
/// 4. Reads size_in_atlas for width/height
/// 5. Calls Item.Create() with primitives
/// 6. Caches in dictionary for O(1) queries
///
/// PERFORMANCE:
/// - Loading happens once at DI container setup (GameStrapper)
/// - All queries are O(1) dictionary lookups or cached list returns
/// - No runtime I/O or Godot resource loading during queries
/// </remarks>
public sealed class TileSetItemRepository : IItemRepository
{
    private readonly Dictionary<ItemId, ItemEntity> _itemsById = new();
    private readonly List<ItemEntity> _allItems = new();
    private readonly ILogger<TileSetItemRepository> _logger;

    public TileSetItemRepository(
        TileSet itemTileSet,
        ILogger<TileSetItemRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (itemTileSet == null)
        {
            throw new ArgumentNullException(nameof(itemTileSet), "Item TileSet cannot be null");
        }

        LoadItemsFromTileSet(itemTileSet);
    }

    private void LoadItemsFromTileSet(TileSet tileSet)
    {
        _logger.LogInformation("Auto-discovering items from TileSet...");

        // Get atlas source 0 (first and only source in item_sprites.tres)
        var sourceId = 0;
        var atlasSource = tileSet.GetSource(sourceId) as TileSetAtlasSource;

        if (atlasSource == null)
        {
            _logger.LogWarning("TileSet has no atlas source at index {SourceId}", sourceId);
            return;
        }

        var tilesCount = atlasSource.GetTilesCount();
        _logger.LogDebug("Found {TilesCount} tiles in TileSet atlas source", tilesCount);

        for (int i = 0; i < tilesCount; i++)
        {
            var tileId = atlasSource.GetTileId(i);
            var result = LoadItemFromTile(atlasSource, tileId);

            if (result.IsSuccess)
            {
                var item = result.Value;
                _itemsById[item.Id] = item;
                _allItems.Add(item);

                _logger.LogDebug(
                    "Loaded item: {Name} (type: {Type}, atlas: {X},{Y}, sprite: {SpriteW}×{SpriteH}, inventory: {InvW}×{InvH}, stack: {MaxStack})",
                    item.Name,
                    item.Type,
                    item.AtlasX,
                    item.AtlasY,
                    item.SpriteWidth,
                    item.SpriteHeight,
                    item.InventoryWidth,
                    item.InventoryHeight,
                    item.MaxStackSize);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to load item at atlas coords {X},{Y}: {Error}",
                    tileId.X,
                    tileId.Y,
                    result.Error);
            }
        }

        _logger.LogInformation(
            "Item catalog loaded: {ItemCount} items discovered",
            _allItems.Count);
    }

    private Result<ItemEntity> LoadItemFromTile(TileSetAtlasSource atlasSource, Vector2I tileCoords)
    {
        // Get tile data (contains custom data layers)
        var tileData = atlasSource.GetTileData(tileCoords, alternativeTile: 0);

        if (tileData == null)
        {
            return Result.Failure<ItemEntity>("TileData is null");
        }

        // Read custom data layers (Phase 0 contract)
        var name = tileData.GetCustomData("item_name").AsString();
        var type = tileData.GetCustomData("item_type").AsString();
        var maxStackSize = tileData.GetCustomData("max_stack_size").AsInt32();

        // Read sprite dimensions from size_in_atlas (for rendering)
        var sizeInAtlas = atlasSource.GetTileSizeInAtlas(tileCoords);
        int spriteWidth = sizeInAtlas.X;
        int spriteHeight = sizeInAtlas.Y;

        // PHASE 4: Read shape resource (complex shapes like L/T-shapes)
        var shapeVariant = tileData.GetCustomData("item_shape");
        ItemShapeResource? shapeResource = null;
        if (shapeVariant.VariantType == Variant.Type.Object)
        {
            shapeResource = shapeVariant.AsGodotObject() as ItemShapeResource;
        }

        // Fallback: Read legacy inventory dimensions (backward compatibility)
        var invWidthVariant = tileData.GetCustomData("inventory_width");
        var invHeightVariant = tileData.GetCustomData("inventory_height");

        int inventoryWidth;
        int inventoryHeight;
        string shapeEncoding;

        if (shapeResource != null)
        {
            // PHASE 4: Use shape resource (complex shapes)
            inventoryWidth = shapeResource.Width;
            inventoryHeight = shapeResource.Height;
            shapeEncoding = shapeResource.ToEncoding();
            _logger.LogDebug(
                "Item {Name}: Using shape resource {Width}×{Height} (encoding: {Encoding})",
                name, inventoryWidth, inventoryHeight, shapeEncoding);
        }
        else
        {
            // BACKWARD COMPATIBILITY: Legacy inventory dimensions (rectangles only)
            inventoryWidth = invWidthVariant.VariantType == Variant.Type.Int
                ? invWidthVariant.AsInt32()
                : spriteWidth;

            inventoryHeight = invHeightVariant.VariantType == Variant.Type.Int
                ? invHeightVariant.AsInt32()
                : spriteHeight;

            shapeEncoding = $"rect:{inventoryWidth}x{inventoryHeight}";
            _logger.LogDebug(
                "Item {Name}: Using legacy dimensions {Width}×{Height} (fallback rectangle)",
                name, inventoryWidth, inventoryHeight);
        }

        // ARCHITECTURE: Parse shape encoding to Domain ItemShape
        var shapeResult = Core.Domain.Common.ItemShape.CreateFromEncoding(
            shapeEncoding, inventoryWidth, inventoryHeight);

        if (shapeResult.IsFailure)
        {
            return Result.Failure<ItemEntity>(
                $"Failed to parse item shape: {shapeResult.Error}");
        }

        // PHASE 4: Use CreateWithShape (explicit ItemShape parameter)
        return ItemEntity.CreateWithShape(
            ItemId.NewId(),
            atlasX: tileCoords.X,
            atlasY: tileCoords.Y,
            name: name,
            type: type,
            spriteWidth: spriteWidth,
            spriteHeight: spriteHeight,
            shape: shapeResult.Value,
            maxStackSize: maxStackSize);
    }

    public Result<ItemEntity> GetById(ItemId itemId)
    {
        if (_itemsById.TryGetValue(itemId, out var item))
        {
            return Result.Success(item);
        }

        return Result.Failure<ItemEntity>($"Item {itemId} not found in catalog");
    }

    public Task<Result<ItemEntity>> GetByIdAsync(ItemId itemId, CancellationToken cancellationToken = default)
    {
        // Sync-over-async wrapper (catalog is in-memory, no I/O)
        return Task.FromResult(GetById(itemId));
    }

    public Result<List<ItemEntity>> GetAll()
    {
        // Return cached list (loaded once at startup)
        return Result.Success(_allItems);
    }

    public Result<List<ItemEntity>> GetByType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return Result.Failure<List<ItemEntity>>("Type cannot be empty");
        }

        var matchingItems = _allItems
            .Where(item => item.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Result.Success(matchingItems);
    }
}
