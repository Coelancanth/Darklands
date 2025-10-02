using Godot;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using Darklands.Core.Features.Item.Application.Queries;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Components;

/// <summary>
/// Godot node that renders an item sprite from the item catalog TileSet.
/// </summary>
/// <remarks>
/// ARCHITECTURE (ADR-002):
/// - Uses IMediator to query item data from catalog
/// - Renders sprite using TileSet atlas coordinates from ItemDto
/// - No direct TileSet dependency (queries provide coordinates)
///
/// IMPLEMENTATION:
/// - Extends TextureRect (Control node) instead of Sprite2D for proper layout
/// - Works correctly with Control containers (CenterContainer, PanelContainer)
/// - Uses ExpandMode.KeepAspectCentered for automatic centering
///
/// USAGE:
/// 1. Add ItemSpriteNode to scene
/// 2. Call DisplayItem(itemId) to show item sprite
/// 3. Sprite automatically renders centered in available space
/// </remarks>
public partial class ItemSpriteNode : TextureRect
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// TileSet resource containing item sprites.
    /// Assign in Godot editor: res://assets/inventory_ref/item_sprites.tres
    /// </summary>
    [Export] public TileSet? ItemTileSet { get; set; }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES (resolved via ServiceLocator in _Ready)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IMediator? _mediator;
    private ILogger<ItemSpriteNode>? _logger;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve dependencies via ServiceLocator (Godot constraint)
        var mediatorResult = ServiceLocator.GetService<IMediator>();
        var loggerResult = ServiceLocator.GetService<ILogger<ItemSpriteNode>>();

        if (mediatorResult.IsFailure || loggerResult.IsFailure)
        {
            GD.PrintErr("[ItemSpriteNode] Failed to resolve dependencies");
            return;
        }

        _mediator = mediatorResult.Value;
        _logger = loggerResult.Value;

        // Validate TileSet assigned
        if (ItemTileSet == null)
        {
            _logger.LogWarning("ItemTileSet not assigned in editor");
            return;
        }

        // Configure TextureRect rendering
        ExpandMode = ExpandModeEnum.IgnoreSize; // Don't auto-resize, use CustomMinimumSize from parent
        StretchMode = StretchModeEnum.KeepAspectCentered; // Center sprite, maintain aspect ratio
        TextureFilter = TextureFilterEnum.Nearest; // Pixel-perfect rendering for pixel art

        _logger.LogDebug("ItemSpriteNode initialized");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PUBLIC API
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Displays an item sprite by querying the item catalog.
    /// </summary>
    /// <param name="itemId">ID of item to display</param>
    public async void DisplayItem(ItemId itemId)
    {
        if (_mediator == null || ItemTileSet == null)
        {
            _logger?.LogWarning("Cannot display item: dependencies not initialized");
            return;
        }

        // Query item data from catalog
        var query = new GetItemByIdQuery(itemId);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            _logger?.LogWarning("Failed to get item {ItemId}: {Error}", itemId, result.Error);
            Visible = false;
            return;
        }

        var itemDto = result.Value;
        RenderItemSprite(itemDto);
    }

    /// <summary>
    /// Displays an item sprite directly from ItemDto (for batch rendering).
    /// </summary>
    /// <param name="itemDto">Item data with atlas coordinates</param>
    public void DisplayItemDto(ItemDto itemDto)
    {
        if (ItemTileSet == null)
        {
            _logger?.LogWarning("Cannot display item: TileSet not assigned");
            return;
        }

        RenderItemSprite(itemDto);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void RenderItemSprite(ItemDto itemDto)
    {
        if (ItemTileSet == null)
        {
            _logger?.LogWarning("Cannot render item {Name}: TileSet not assigned", itemDto.Name);
            return;
        }

        // Get atlas source (first and only source in item_sprites.tres)
        var atlasSource = ItemTileSet.GetSource(0) as TileSetAtlasSource;

        if (atlasSource == null)
        {
            _logger?.LogError("ItemTileSet has no atlas source at index 0");
            Visible = false;
            return;
        }

        // Create AtlasTexture for this specific tile region
        var tileCoords = new Vector2I(itemDto.AtlasX, itemDto.AtlasY);
        var region = atlasSource.GetTileTextureRegion(tileCoords);

        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        // Set the AtlasTexture directly
        Texture = atlasTexture;
        Visible = true;

        _logger?.LogDebug(
            "Rendered item sprite: {Name} (atlas: {X},{Y}, region: {Region})",
            itemDto.Name,
            itemDto.AtlasX,
            itemDto.AtlasY,
            region);
    }
}
