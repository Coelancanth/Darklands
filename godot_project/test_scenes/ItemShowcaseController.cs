using Darklands.Components;
using Darklands.Core.Features.Item.Application.Queries;
using Darklands.Core.Infrastructure.DependencyInjection;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands;

/// <summary>
/// Controller for Item Showcase scene - demonstrates item catalog auto-discovery.
/// </summary>
/// <remarks>
/// PURPOSE (VS_009 Phase 4):
/// - Validates TileSetItemRepository auto-discovery works correctly
/// - Displays all items from catalog with sprites and metadata
/// - Demonstrates that adding items to TileSet automatically appears (zero code!)
///
/// ARCHITECTURE (ADR-002):
/// - Uses ServiceLocator in _Ready() (Godot constraint)
/// - Queries item catalog via MediatR
/// - Renders items using ItemSpriteNode components
/// </remarks>
public partial class ItemShowcaseController : Control
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// TileSet resource containing item sprites.
    /// Assign: res://assets/inventory_ref/item_sprites.tres
    /// </summary>
    [Export] public TileSet? ItemTileSet { get; set; }

    /// <summary>
    /// Container for item display (VBoxContainer or GridContainer).
    /// Items will be added as children.
    /// </summary>
    [Export] public NodePath? ItemContainerPath { get; set; }

    /// <summary>
    /// Scene to instantiate for each item (should contain ItemSpriteNode + Label).
    /// </summary>
    [Export] public PackedScene? ItemDisplayScene { get; set; }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IMediator _mediator = null!;
    private ILogger<ItemShowcaseController> _logger = null!;
    private Node? _itemContainer;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve dependencies via ServiceLocator
        var mediatorResult = ServiceLocator.GetService<IMediator>();
        var loggerResult = ServiceLocator.GetService<ILogger<ItemShowcaseController>>();

        if (mediatorResult.IsFailure || loggerResult.IsFailure)
        {
            GD.PrintErr("[ItemShowcaseController] Failed to resolve dependencies");
            return;
        }

        _mediator = mediatorResult.Value;
        _logger = loggerResult.Value;

        // Resolve item container
        if (ItemContainerPath != null)
        {
            _itemContainer = GetNode(ItemContainerPath);
        }

        if (_itemContainer == null)
        {
            _logger.LogError("Item container not assigned or not found");
            return;
        }

        if (ItemTileSet == null)
        {
            _logger.LogError("ItemTileSet not assigned in editor");
            return;
        }

        _logger.LogInformation("ItemShowcaseController initialized - loading items...");

        // Load and display all items from catalog
        LoadAndDisplayItems();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async void LoadAndDisplayItems()
    {
        // Query all items from catalog
        var query = new GetAllItemsQuery();
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to load items: {Error}", result.Error);
            return;
        }

        var items = result.Value;
        _logger.LogInformation("Loaded {Count} items from catalog", items.Count);

        // Display each item
        foreach (var itemDto in items)
        {
            DisplayItem(itemDto);
        }

        _logger.LogInformation("Item showcase populated with {Count} items", items.Count);
    }

    private void DisplayItem(ItemDto itemDto)
    {
        // Create item display panel
        var itemPanel = new PanelContainer();
        itemPanel.CustomMinimumSize = new Vector2(200, 120);

        var vbox = new VBoxContainer();
        itemPanel.AddChild(vbox);

        // Create sprite node
        var sprite = new ItemSpriteNode
        {
            ItemTileSet = ItemTileSet,
            ItemScale = 2.0f // 2x scale for better visibility
        };

        // Call _Ready manually (Godot won't call it for programmatically added nodes until next frame)
        sprite._Ready();

        // Display item sprite
        sprite.DisplayItemDto(itemDto);

        // Center sprite in container
        var spriteContainer = new CenterContainer();
        spriteContainer.CustomMinimumSize = new Vector2(200, 80);
        spriteContainer.AddChild(sprite);
        vbox.AddChild(spriteContainer);

        // Add item name label
        var nameLabel = new Label
        {
            Text = itemDto.Name,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(nameLabel);

        // Add item metadata label
        var metadataLabel = new Label
        {
            Text = $"Type: {itemDto.Type} | Size: {itemDto.Width}x{itemDto.Height} | Stack: {itemDto.MaxStackSize}",
            HorizontalAlignment = HorizontalAlignment.Center,
            Theme = new Theme()
        };

        // Make metadata text smaller
        metadataLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(metadataLabel);

        // Add to container
        _itemContainer?.AddChild(itemPanel);

        _logger.LogDebug("Displayed item: {Name} ({Type})", itemDto.Name, itemDto.Type);
    }
}
