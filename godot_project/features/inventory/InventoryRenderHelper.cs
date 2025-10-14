using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application.Queries;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Presentation.Features.Inventory;

/// <summary>
/// Static helper for shared inventory rendering logic (DRY principle).
/// </summary>
/// <remarks>
/// TD_003 Phase 2: Extracted from EquipmentSlotNode to eliminate duplication.
///
/// DESIGN PRINCIPLES:
/// - Static methods (no state, pure functions)
/// - Dependency injection via parameters (IMediator, TileSet, CellSize)
/// - Returns Godot nodes (caller manages AddChild/QueueFree lifecycle)
/// - Testable (no hidden dependencies, explicit parameters)
///
/// SHARED BY:
/// - EquipmentSlotNode (equipment slots - weapon, armor, ring)
/// - InventoryContainerNode (Tetris grids - backpacks, chests) [Phase 3]
///
/// WHY STATIC HELPER (not base class):
/// - Simpler: No inheritance hierarchy
/// - Flexible: Any component can use (composition > inheritance)
/// - Focused: Pure rendering utilities (no state management)
/// </remarks>
public static class InventoryRenderHelper
{
    // TileSet atlas coordinates for highlight sprites
    private static readonly Vector2I HIGHLIGHT_GREEN_COORDS = new(1, 6);
    private static readonly Vector2I HIGHLIGHT_RED_COORDS = new(1, 7);

    /// <summary>
    /// Creates a TextureRect for rendering an item sprite (scaled and centered).
    /// </summary>
    /// <param name="itemId">Item to render</param>
    /// <param name="mediator">MediatR instance for querying item data</param>
    /// <param name="itemTileSet">TileSet containing item sprites</param>
    /// <param name="cellSize">Cell size in pixels (for centering calculation)</param>
    /// <param name="shouldCenter">If true, center sprite in cell; if false, position at (0,0)</param>
    /// <param name="rotation">Sprite rotation in radians (default 0)</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>TextureRect ready to AddChild, or null if rendering failed</returns>
    /// <remarks>
    /// EQUIPMENT SLOTS: shouldCenter=true (centered, scaled to fit 96×96 cell)
    /// INVENTORY GRIDS: shouldCenter=false (origin-aligned, multi-cell L-shapes)
    /// </remarks>
    public static async Task<TextureRect?> CreateItemSpriteAsync(
        ItemId itemId,
        IMediator mediator,
        TileSet? itemTileSet,
        int cellSize,
        bool shouldCenter = true,
        float rotation = 0f,
        ILogger? logger = null)
    {
        // Query item data
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = await mediator.Send(itemQuery);

        if (itemResult.IsFailure)
        {
            logger?.LogWarning("Failed to load item {ItemId}: {Error}", itemId, itemResult.Error);
            return null;
        }

        if (itemTileSet == null)
        {
            logger?.LogWarning("No TileSet provided for rendering item {ItemId}", itemId);
            return null;
        }

        var item = itemResult.Value;
        var atlasSource = itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
        {
            logger?.LogWarning("TileSet has no TileSetAtlasSource");
            return null;
        }

        // Extract sprite region from atlas
        var tileCoords = new Vector2I(item.AtlasX, item.AtlasY);
        var region = atlasSource.GetTileTextureRegion(tileCoords);

        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        // Calculate scaling and positioning
        float actualTextureWidth = region.Size.X;
        float actualTextureHeight = region.Size.Y;

        float textureWidth, textureHeight, texturePosX, texturePosY;

        if (shouldCenter)
        {
            // Equipment slot mode: Scale to fit cell, center sprite
            float scale = Math.Min(cellSize / actualTextureWidth, cellSize / actualTextureHeight);
            textureWidth = actualTextureWidth * scale;
            textureHeight = actualTextureHeight * scale;
            texturePosX = (cellSize - textureWidth) / 2f;
            texturePosY = (cellSize - textureHeight) / 2f;
        }
        else
        {
            // Inventory grid mode: No scaling, origin-aligned
            textureWidth = actualTextureWidth;
            textureHeight = actualTextureHeight;
            texturePosX = 0f;
            texturePosY = 0f;
        }

        var textureRect = new TextureRect
        {
            Name = $"Item_{itemId.Value}",
            Texture = atlasTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(textureWidth, textureHeight),
            Size = new Vector2(textureWidth, textureHeight),
            Position = new Vector2(texturePosX, texturePosY),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Rotation = rotation,
            ZIndex = 100
        };

        logger?.LogDebug("Created sprite for item {ItemId}: {W}×{H} at ({X},{Y})",
            itemId, textureWidth, textureHeight, texturePosX, texturePosY);

        return textureRect;
    }

    /// <summary>
    /// Creates a drag preview Control for an item.
    /// </summary>
    /// <param name="itemId">Item being dragged</param>
    /// <param name="itemName">Item name (fallback if sprite fails to load)</param>
    /// <param name="mediator">MediatR instance for querying item data</param>
    /// <param name="itemTileSet">TileSet containing item sprites</param>
    /// <param name="cellSize">Base cell size (preview scaled to 0.8× for visual clarity)</param>
    /// <returns>Control node ready to use as drag preview, or Label fallback</returns>
    public static Control CreateDragPreview(
        ItemId itemId,
        string itemName,
        IMediator mediator,
        TileSet? itemTileSet,
        int cellSize)
    {
        // Query item (blocking OK for drag preview - synchronous API)
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = mediator.Send(itemQuery).Result;

        if (itemResult.IsFailure || itemTileSet == null)
        {
            return new Label { Text = itemName };
        }

        var item = itemResult.Value;
        var atlasSource = itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
        {
            return new Label { Text = item.Name };
        }

        // Extract sprite
        var tileCoords = new Vector2I(item.AtlasX, item.AtlasY);
        var region = atlasSource.GetTileTextureRegion(tileCoords);

        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        // Drag preview size (80% of cell for visual clarity)
        float previewSize = cellSize * 0.8f;

        var sprite = new TextureRect
        {
            Texture = atlasTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(previewSize, previewSize),
            Size = new Vector2(previewSize, previewSize),
            Modulate = new Color(1, 1, 1, 0.8f) // Semi-transparent
        };

        // Center preview at cursor
        var previewRoot = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        var offsetContainer = new Control
        {
            Position = new Vector2(-previewSize / 2f, -previewSize / 2f),
            CustomMinimumSize = new Vector2(previewSize, previewSize)
        };
        offsetContainer.AddChild(sprite);
        previewRoot.AddChild(offsetContainer);

        return previewRoot;
    }

    /// <summary>
    /// Creates a highlight TextureRect (green for valid drop, red for invalid).
    /// </summary>
    /// <param name="isValid">True for green (valid drop), false for red (invalid drop)</param>
    /// <param name="itemTileSet">TileSet containing highlight sprites</param>
    /// <param name="cellSize">Cell size for highlight dimensions</param>
    /// <param name="opacity">Highlight opacity (default 0.4)</param>
    /// <returns>TextureRect ready to AddChild to highlight overlay, or null if TileSet unavailable</returns>
    public static TextureRect? CreateHighlight(
        bool isValid,
        TileSet? itemTileSet,
        int cellSize,
        float opacity = 0.4f)
    {
        if (itemTileSet == null)
            return null;

        var atlasSource = itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
            return null;

        var highlightCoords = isValid ? HIGHLIGHT_GREEN_COORDS : HIGHLIGHT_RED_COORDS;
        var region = atlasSource.GetTileTextureRegion(highlightCoords);

        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        var highlight = new TextureRect
        {
            Name = "Highlight",
            Texture = atlasTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(cellSize, cellSize),
            Position = Vector2.Zero,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1, 1, 1, opacity)
        };

        return highlight;
    }
}
