using System;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Domain;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Components;

/// <summary>
/// Single equipment slot (weapon/armor) with type filtering.
/// Displays one item's full sprite, regardless of grid size.
/// </summary>
/// <remarks>
/// DESIGN DIFFERENCE vs SpatialInventoryContainerNode:
/// - Equipment slots hold ONE item (not a grid)
/// - Renders full item sprite in slot area (2×4 sword shows full sprite)
/// - Drag-drop onto slot (no grid positioning)
/// - Type-filtered (weapon slot rejects potions)
///
/// PHASE 1 SCOPE:
/// - Single item display
/// - Type filtering (WeaponOnly)
/// - Drag-drop integration
/// - No sprite rendering yet (Phase 1 limitation)
/// </remarks>
public partial class EquipmentSlotNode : Control
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Actor ID for this equipment slot (assign via code from parent controller).
    /// </summary>
    public ActorId? OwnerActorId { get; set; }

    /// <summary>
    /// Slot title (displayed below slot area).
    /// </summary>
    [Export] public string SlotTitle { get; set; } = "Weapon";

    /// <summary>
    /// Slot size in pixels (default: 96×96 for weapon display).
    /// </summary>
    [Export] public int SlotSize { get; set; } = 96;

    /// <summary>
    /// Container type for filtering (WeaponOnly, ArmorOnly, etc.).
    /// </summary>
    public ContainerType ContainerType { get; set; } = ContainerType.WeaponOnly;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES (injected via properties before AddChild)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public IMediator? Mediator { get; set; }
    public TileSet? ItemTileSet { get; set; }

    private IMediator _mediator = null!;
    private ILogger<EquipmentSlotNode> _logger = null!;
    private TileSet? _itemTileSet;

    // Slot state
    private ItemId? _equippedItemId;

    // UI nodes
    private Label? _titleLabel;
    private Panel? _slotPanel;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve logger
        var loggerResult = Darklands.Core.Infrastructure.DependencyInjection.ServiceLocator
            .GetService<ILogger<EquipmentSlotNode>>();

        if (loggerResult.IsFailure)
        {
            GD.PrintErr("[EquipmentSlotNode] Failed to resolve logger");
            return;
        }

        _logger = loggerResult.Value;

        // Use dependencies injected via properties
        if (Mediator == null)
        {
            _logger.LogError("Mediator not injected (set via property before AddChild)");
            return;
        }

        _mediator = Mediator;
        _itemTileSet = ItemTileSet; // Optional for Phase 1

        if (OwnerActorId == null)
        {
            _logger.LogError("OwnerActorId not assigned");
            return;
        }

        // Build UI
        BuildUI();

        // Load slot data
        LoadSlotAsync();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DRAG-DROP SYSTEM (Godot built-in)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (_equippedItemId == null)
            return default; // No item equipped

        // Create drag preview
        var preview = new Label
        {
            Text = "⚔️ Weapon",
            Modulate = new Color(1, 1, 1, 0.7f)
        };
        SetDragPreview(preview);

        // Return drag data
        var dragData = new Godot.Collections.Dictionary
        {
            ["itemIdGuid"] = _equippedItemId.Value.Value.ToString(),
            ["sourceActorIdGuid"] = OwnerActorId?.Value.ToString() ?? string.Empty,
            ["sourceX"] = 0, // Equipment slots don't use grid positions
            ["sourceY"] = 0
        };

        return dragData;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
            return false;

        var dragData = data.AsGodotDictionary();
        if (!dragData.ContainsKey("itemIdGuid"))
            return false;

        // Equipment slots accept drops if empty
        // Type filtering validated in MoveItemBetweenContainersCommand handler
        return _equippedItemId == null;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dragData = data.AsGodotDictionary();
        var itemIdGuidStr = dragData["itemIdGuid"].AsString();
        var sourceActorIdGuidStr = dragData["sourceActorIdGuid"].AsString();

        if (!Guid.TryParse(itemIdGuidStr, out var itemIdGuid) ||
            !Guid.TryParse(sourceActorIdGuidStr, out var sourceActorIdGuid))
        {
            _logger.LogError("Failed to parse drag data GUIDs");
            return;
        }

        var itemId = new ItemId(itemIdGuid);
        var sourceActorId = new ActorId(sourceActorIdGuid);

        // Move item to this equipment slot (position 0,0 - slots don't use grid)
        MoveItemAsync(sourceActorId, itemId);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void BuildUI()
    {
        var vbox = new VBoxContainer();
        // WHY: Pass allows events to reach Panel while also bubbling to parent
        vbox.MouseFilter = MouseFilterEnum.Pass;
        AddChild(vbox);

        // Slot panel (large square for item display)
        _slotPanel = new Panel
        {
            CustomMinimumSize = new Vector2(SlotSize, SlotSize),
            // WHY: Stop makes Panel receive mouse events for drag-drop
            MouseFilter = MouseFilterEnum.Stop
        };

        // Styling (dark background with border)
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.15f),
            BorderColor = new Color(0.5f, 0.5f, 0.5f)
        };
        style.SetBorderWidthAll(2);
        _slotPanel.AddThemeStyleboxOverride("panel", style);

        vbox.AddChild(_slotPanel);

        // Title label below slot
        _titleLabel = new Label
        {
            Text = SlotTitle,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(_titleLabel);
    }

    private async void LoadSlotAsync()
    {
        if (OwnerActorId == null)
            return;

        var query = new GetInventoryQuery(OwnerActorId.Value);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to load equipment slot: {Error}", result.Error);
            return;
        }

        var inventory = result.Value;

        // Equipment slots hold max 1 item
        _equippedItemId = inventory.Count > 0 ? inventory.Items[0] : null;

        // Update title with equipped status
        if (_titleLabel != null)
        {
            _titleLabel.Text = _equippedItemId != null
                ? $"{SlotTitle} (Equipped)"
                : $"{SlotTitle} (Empty)";
        }

        _logger.LogDebug("{SlotTitle} loaded: {Status}",
            SlotTitle,
            _equippedItemId != null ? "Equipped" : "Empty");
    }

    private async void MoveItemAsync(ActorId sourceActorId, ItemId itemId)
    {
        if (OwnerActorId == null)
            return;

        var command = new MoveItemBetweenContainersCommand(
            sourceActorId,
            OwnerActorId.Value,
            itemId,
            new GridPosition(0, 0)); // Equipment slots use (0,0) as placeholder position

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to equip item: {Error}", result.Error);
            return;
        }

        _logger.LogInformation("Item equipped to {SlotTitle}", SlotTitle);

        // Reload slot to reflect changes
        LoadSlotAsync();
    }
}
