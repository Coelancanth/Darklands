using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Domain;
using Darklands.Core.Features.Item.Application.Queries;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Components;

/// <summary>
/// Spatial inventory container with drag-drop support (VS_018 Phase 1).
/// Renders inventory as grid, handles drag-drop via Godot's built-in drag system.
/// </summary>
/// <remarks>
/// ARCHITECTURE:
/// - Gets IMediator from parent SpatialInventoryTestController (avoids duplicate ServiceLocator)
/// - Queries inventory state via GetInventoryQuery
/// - Sends commands via PlaceItemAtPositionCommand, MoveItemBetweenContainersCommand
/// - Drag-drop uses Godot's `_GetDragData`, `_CanDropData`, `_DropData` pattern
///
/// PHASE 1 SCOPE:
/// - All items treated as 1×1 (multi-cell in Phase 2)
/// - Type filtering (weapon slots reject potions)
/// - Visual feedback (green = valid, red = invalid)
/// </remarks>
public partial class SpatialInventoryContainerNode : Control
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT SIGNALS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Emitted when inventory contents change (item moved/added/removed).
    /// WHY: Parent controller subscribes to refresh ALL containers (cross-container sync).
    /// </summary>
    [Signal]
    public delegate void InventoryChangedEventHandler();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Actor ID for this inventory (assign via code from parent controller).
    /// </summary>
    public ActorId? OwnerActorId { get; set; }

    /// <summary>
    /// Container title (displayed above grid).
    /// </summary>
    [Export] public string ContainerTitle { get; set; } = "Inventory";

    /// <summary>
    /// Cell size in pixels (default: 48×48).
    /// </summary>
    [Export] public int CellSize { get; set; } = 48;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES (injected via properties before AddChild)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public IMediator? Mediator { get; set; }
    public TileSet? ItemTileSet { get; set; }

    private IMediator _mediator = null!;
    private ILogger<SpatialInventoryContainerNode> _logger = null!;
    private TileSet? _itemTileSet;

    // Grid state
    private int _gridWidth;
    private int _gridHeight;
    private ContainerType _containerType = ContainerType.General;
    private Dictionary<GridPosition, ItemId> _itemsAtPositions = new();
    private Dictionary<ItemId, string> _itemTypes = new(); // Cache item types for color coding
    private Dictionary<ItemId, string> _itemNames = new(); // Cache item names for tooltips

    // UI nodes
    private Label? _titleLabel;
    private GridContainer? _gridContainer;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve logger
        var loggerResult = Darklands.Core.Infrastructure.DependencyInjection.ServiceLocator
            .GetService<ILogger<SpatialInventoryContainerNode>>();

        if (loggerResult.IsFailure)
        {
            GD.PrintErr("[SpatialInventoryContainerNode] Failed to resolve logger");
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

        // Load inventory data
        LoadInventoryAsync();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DRAG-DROP SYSTEM (Godot built-in)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override Variant _GetDragData(Vector2 atPosition)
    {
        _logger.LogDebug("_GetDragData called at position ({X}, {Y})", atPosition.X, atPosition.Y);

        // Find which grid cell was clicked
        var gridPos = PixelToGridPosition(atPosition);

        if (gridPos == null)
        {
            _logger.LogDebug("PixelToGridPosition returned null (outside grid bounds)");
            return default;
        }

        _logger.LogDebug("Grid position: ({GridX}, {GridY})", gridPos.Value.X, gridPos.Value.Y);

        if (!_itemsAtPositions.ContainsKey(gridPos.Value))
        {
            _logger.LogDebug("No item at grid position ({GridX}, {GridY})", gridPos.Value.X, gridPos.Value.Y);
            return default; // No item at this position
        }

        var itemId = _itemsAtPositions[gridPos.Value];
        _logger.LogInformation("Starting drag: Item {ItemId} from {Container} at ({X}, {Y})",
            itemId, ContainerTitle, gridPos.Value.X, gridPos.Value.Y);

        // Create drag preview with item name (visual feedback)
        string itemName = _itemNames.GetValueOrDefault(itemId, "Item");
        string itemType = _itemTypes.GetValueOrDefault(itemId, "?");

        var preview = new Label
        {
            Text = $"📦 {itemName}",
            Modulate = new Color(1, 1, 1, 0.8f)
        };
        preview.AddThemeFontSizeOverride("font_size", 16);

        // Add background to preview for better visibility
        var previewBg = new Panel();
        var previewStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        previewStyle.SetContentMarginAll(8);
        previewBg.AddThemeStyleboxOverride("panel", previewStyle);
        previewBg.AddChild(preview);

        SetDragPreview(previewBg);

        // Return drag data using Guids (simpler than value objects)
        var dragData = new Godot.Collections.Dictionary
        {
            ["itemIdGuid"] = itemId.Value.ToString(),
            ["sourceActorIdGuid"] = OwnerActorId?.Value.ToString() ?? string.Empty,
            ["sourceX"] = gridPos.Value.X,
            ["sourceY"] = gridPos.Value.Y
        };

        _logger.LogDebug("Drag data created successfully");
        return dragData;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        _logger.LogDebug("_CanDropData called at position ({X}, {Y})", atPosition.X, atPosition.Y);

        if (data.VariantType != Variant.Type.Dictionary)
        {
            _logger.LogDebug("Invalid drag data type: {Type}", data.VariantType);
            return false;
        }

        var dragData = data.AsGodotDictionary();
        if (!dragData.ContainsKey("itemIdGuid"))
        {
            _logger.LogDebug("Drag data missing itemIdGuid key");
            return false;
        }

        // Find target grid position
        var targetPos = PixelToGridPosition(atPosition);
        if (targetPos == null)
        {
            _logger.LogDebug("Target position outside grid bounds");
            return false;
        }

        // Check if position is occupied
        bool isOccupied = _itemsAtPositions.ContainsKey(targetPos.Value);

        // SWAP SUPPORT (Option C): Equipment slots allow swapping, backpacks don't
        if (isOccupied)
        {
            bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;

            if (isEquipmentSlot)
            {
                _logger.LogDebug("Position ({X}, {Y}) occupied - swap allowed (equipment slot)",
                    targetPos.Value.X, targetPos.Value.Y);
                // Swap validation happens in SwapItemsAsync (pre-validated before any removal)
            }
            else
            {
                _logger.LogDebug("Position ({X}, {Y}) occupied - swap NOT allowed (backpack)",
                    targetPos.Value.X, targetPos.Value.Y);
                return false;
            }
        }

        // Type validation for specialized containers (prevent data loss bug)
        // WHY: Must validate BEFORE Godot removes item from source container
        if (OwnerActorId != null)
        {
            var itemIdGuidStr = dragData["itemIdGuid"].AsString();
            if (Guid.TryParse(itemIdGuidStr, out var itemIdGuid))
            {
                var itemId = new ItemId(itemIdGuid);

                // Query item type (synchronous lookup from cache)
                if (_itemTypes.TryGetValue(itemId, out var itemType))
                {
                    // Check type compatibility with this container
                    if (!CanAcceptItemType(itemType))
                    {
                        _logger.LogDebug("Item type '{ItemType}' rejected by container type filter", itemType);
                        return false;
                    }
                }
                else
                {
                    // Item type not in cache - query via MediatR (blocks, but necessary)
                    var itemTypeResult = GetItemTypeAsync(itemId).Result;
                    if (itemTypeResult.IsSuccess)
                    {
                        if (!CanAcceptItemType(itemTypeResult.Value))
                        {
                            _logger.LogDebug("Item type '{ItemType}' rejected by container type filter", itemTypeResult.Value);
                            return false;
                        }
                    }
                }
            }
        }

        _logger.LogDebug("Can drop at ({X}, {Y}): true", targetPos.Value.X, targetPos.Value.Y);
        return true;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        _logger.LogInformation("_DropData called at position ({X}, {Y})", atPosition.X, atPosition.Y);

        var dragData = data.AsGodotDictionary();
        var itemIdGuidStr = dragData["itemIdGuid"].AsString();
        var sourceActorIdGuidStr = dragData["sourceActorIdGuid"].AsString();

        _logger.LogDebug("Parsing GUIDs: ItemId={ItemGuid}, SourceActor={ActorGuid}",
            itemIdGuidStr, sourceActorIdGuidStr);

        if (!Guid.TryParse(itemIdGuidStr, out var itemIdGuid) ||
            !Guid.TryParse(sourceActorIdGuidStr, out var sourceActorIdGuid))
        {
            _logger.LogError("Failed to parse drag data GUIDs");
            return;
        }

        var targetPos = PixelToGridPosition(atPosition);
        if (targetPos == null)
        {
            _logger.LogError("Target position null after drop");
            return;
        }

        // Reconstruct value objects from Guids
        var itemId = new ItemId(itemIdGuid);
        var sourceActorId = new ActorId(sourceActorIdGuid);

        // Check if this is a SWAP operation (equipment slot with occupied position)
        bool isOccupied = _itemsAtPositions.TryGetValue(targetPos.Value, out var targetItemId);
        bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;

        if (isOccupied && isEquipmentSlot)
        {
            // SAFE SWAP: Validate + Remove + Place with rollback on failure
            var sourceX = dragData["sourceX"].AsInt32();
            var sourceY = dragData["sourceY"].AsInt32();
            var sourcePos = new GridPosition(sourceX, sourceY);

            _logger.LogInformation("Initiating safe swap: {ItemA} ↔ {ItemB}",
                itemId, targetItemId);

            SwapItemsSafeAsync(sourceActorId, itemId, sourcePos, OwnerActorId!.Value, targetItemId, targetPos.Value);
        }
        else
        {
            // REGULAR MOVE: Position is free
            _logger.LogInformation("Drop confirmed: Moving item {ItemId} to ({X}, {Y})",
                itemId, targetPos.Value.X, targetPos.Value.Y);

            MoveItemAsync(sourceActorId, itemId, targetPos.Value);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void BuildUI()
    {
        var vbox = new VBoxContainer();
        // WHY: Pass allows events to reach children (Panel cells) while also reaching parent
        vbox.MouseFilter = MouseFilterEnum.Pass;
        AddChild(vbox);

        // Title
        _titleLabel = new Label
        {
            Text = ContainerTitle,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(_titleLabel);

        // Grid container (will be populated after loading inventory data)
        _gridContainer = new GridContainer
        {
            CustomMinimumSize = new Vector2(CellSize, CellSize),
            // WHY: Pass allows events to reach Panel cells while also bubbling to parent
            MouseFilter = MouseFilterEnum.Pass
        };
        _gridContainer.AddThemeConstantOverride("h_separation", 2);
        _gridContainer.AddThemeConstantOverride("v_separation", 2);
        vbox.AddChild(_gridContainer);
    }

    private async void LoadInventoryAsync()
    {
        if (OwnerActorId == null)
            return;

        var query = new GetInventoryQuery(OwnerActorId.Value);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to load inventory: {Error}", result.Error);
            return;
        }

        var inventory = result.Value;
        _gridWidth = inventory.GridWidth;
        _gridHeight = inventory.GridHeight;
        _containerType = inventory.ContainerType;

        // Update title with capacity info
        if (_titleLabel != null)
        {
            _titleLabel.Text = $"{ContainerTitle} ({inventory.Count}/{inventory.Capacity})";
        }

        // Build grid cells (only if not already built)
        if (_gridContainer != null && _gridContainer.GetChildCount() == 0)
        {
            _gridContainer.Columns = _gridWidth;

            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    var cell = CreateGridCell(new GridPosition(x, y));
                    _gridContainer.AddChild(cell);
                }
            }

            _logger.LogDebug("Created {Count} grid cells ({Width}×{Height})",
                _gridContainer.GetChildCount(), _gridWidth, _gridHeight);
        }

        // Populate items
        _itemsAtPositions.Clear();
        _itemTypes.Clear();
        foreach (var placement in inventory.ItemPlacements)
        {
            _itemsAtPositions[placement.Value] = placement.Key;
        }

        // Query item types for color coding
        await LoadItemTypes();

        RefreshGridDisplay();
    }

    private async Task LoadItemTypes()
    {
        // Query item details to determine types (weapon, item, etc.) and names (for tooltips)
        foreach (var itemId in _itemsAtPositions.Values.Distinct())
        {
            var query = new GetItemByIdQuery(itemId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _itemTypes[itemId] = result.Value.Type;
                _itemNames[itemId] = result.Value.Name;
                _logger.LogDebug("Item {ItemId}: {Name} ({Type})", itemId, result.Value.Name, result.Value.Type);
            }
        }
    }

    private Control CreateGridCell(GridPosition pos)
    {
        var cell = new Panel
        {
            CustomMinimumSize = new Vector2(CellSize, CellSize),
            TooltipText = $"Grid ({pos.X}, {pos.Y})",
            // WHY: Pass allows tooltips to work AND events bubble to parent for drag-drop
            MouseFilter = MouseFilterEnum.Pass
        };

        // Add visual styling (uniform gray for all cells - colors show ITEMS, not cells)
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.2f),
            BorderColor = new Color(0.4f, 0.4f, 0.4f)
        };
        style.SetBorderWidthAll(1);
        cell.AddThemeStyleboxOverride("panel", style);

        return cell;
    }

    private void RefreshGridDisplay()
    {
        // WHY: Phase 1 visual feedback - highlight occupied cells (no sprites yet)
        if (_gridContainer == null)
            return;

        // Update each grid cell's visual state and tooltip
        for (int i = 0; i < _gridContainer.GetChildCount(); i++)
        {
            if (_gridContainer.GetChild(i) is Panel cell)
            {
                // Calculate grid position for this cell index
                int x = i % _gridWidth;
                int y = i / _gridWidth;
                var gridPos = new GridPosition(x, y);

                // Update tooltip based on cell contents
                if (_itemsAtPositions.TryGetValue(gridPos, out var itemId))
                {
                    // Occupied cell: Show item name and type
                    string itemName = _itemNames.GetValueOrDefault(itemId, "Unknown");
                    string itemType = _itemTypes.GetValueOrDefault(itemId, "unknown");
                    cell.TooltipText = $"{itemName} ({itemType})";

                    // Render colored icon INSIDE cell (Phase 1 placeholder for sprites)
                    // WHY: Each item gets unique color for visual distinction
                    RenderItemIcon(cell, itemName);
                }
                else
                {
                    // Empty cell: Show grid position, clear any existing icon
                    cell.TooltipText = $"Empty ({gridPos.X}, {gridPos.Y})";
                    ClearItemIcon(cell);
                }
            }
        }

        _logger.LogDebug("{ContainerTitle}: {ItemCount} items displayed",
            ContainerTitle, _itemsAtPositions.Count);
    }

    /// <summary>
    /// Renders a colored icon inside a cell to represent an item (Phase 1 sprite placeholder).
    /// Each item gets a unique color based on its name.
    /// </summary>
    private void RenderItemIcon(Panel cell, string itemName)
    {
        // Check if icon already exists
        var existingIcon = cell.GetNodeOrNull<ColorRect>("ItemIcon");
        if (existingIcon != null)
        {
            // Update existing icon color
            existingIcon.Color = GetItemColor(itemName);
            return;
        }

        // Create new icon (centered ColorRect)
        var icon = new ColorRect
        {
            Name = "ItemIcon",
            Color = GetItemColor(itemName),
            CustomMinimumSize = new Vector2(CellSize * 0.7f, CellSize * 0.7f),
            Position = new Vector2(CellSize * 0.15f, CellSize * 0.15f), // Center with 15% margin
            MouseFilter = MouseFilterEnum.Ignore // Let parent cell handle mouse events
        };

        cell.AddChild(icon);
    }

    /// <summary>
    /// Clears the item icon from a cell (when cell becomes empty).
    /// </summary>
    private void ClearItemIcon(Panel cell)
    {
        var icon = cell.GetNodeOrNull<ColorRect>("ItemIcon");
        if (icon != null)
        {
            icon.QueueFree();
        }
    }

    /// <summary>
    /// Assigns a unique color to each item based on its name.
    /// WHY: Visual distinction - dagger vs ray_gun get different colors.
    /// </summary>
    private Color GetItemColor(string itemName)
    {
        // Per-item color assignment (unique color for each item, not type)
        return itemName switch
        {
            "dagger" => new Color(0.4f, 0.6f, 1.0f),      // Light blue
            "ray_gun" => new Color(0.7f, 0.3f, 0.9f),     // Purple
            "baton" => new Color(0.5f, 0.5f, 0.5f),       // Gray
            "red_vial" => new Color(1.0f, 0.3f, 0.3f),    // Red
            "green_vial" => new Color(0.3f, 1.0f, 0.5f),  // Bright green
            "gadget" => new Color(1.0f, 0.8f, 0.2f),      // Yellow/gold
            _ => new Color(0.8f, 0.8f, 0.8f)              // White (unknown items)
        };
    }

    private GridPosition? PixelToGridPosition(Vector2 pixelPos)
    {
        if (_gridContainer == null)
            return null;

        // Convert local position to global, then hit-test against grid cell rects
        var selfGlobalPos = GetGlobalRect().Position;
        var globalPoint = selfGlobalPos + pixelPos;

        for (int i = 0; i < _gridContainer.GetChildCount(); i++)
        {
            if (_gridContainer.GetChild(i) is Control cell)
            {
                var rect = cell.GetGlobalRect();
                if (rect.HasPoint(globalPoint))
                {
                    int x = i % _gridWidth;
                    int y = i / _gridWidth;
                    return new GridPosition(x, y);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Safely swaps two items using Remove+Place pattern with full rollback on failure.
    /// WHY: Equipment slots need swap for UX, but must prevent data loss at all costs.
    /// </summary>
    private async void SwapItemsSafeAsync(
        ActorId sourceActorId,
        ItemId sourceItemId,
        GridPosition sourcePos,
        ActorId targetActorId,
        ItemId targetItemId,
        GridPosition targetPos)
    {
        _logger.LogInformation("SAFE SWAP: {SourceItem} @ ({SourceX},{SourceY}) ↔ {TargetItem} @ ({TargetX},{TargetY})",
            sourceItemId, sourcePos.X, sourcePos.Y,
            targetItemId, targetPos.X, targetPos.Y);

        // STEP 1: Remove both items (hold in memory for rollback)
        var removeSourceCmd = new RemoveItemCommand(sourceActorId, sourceItemId);
        var removeTargetCmd = new RemoveItemCommand(targetActorId, targetItemId);

        var removeSourceResult = await _mediator.Send(removeSourceCmd);
        if (removeSourceResult.IsFailure)
        {
            _logger.LogError("Swap aborted: Failed to remove source item: {Error}", removeSourceResult.Error);
            return; // Nothing removed yet, safe to abort
        }

        var removeTargetResult = await _mediator.Send(removeTargetCmd);
        if (removeTargetResult.IsFailure)
        {
            _logger.LogError("Swap aborted: Failed to remove target item: {Error}", removeTargetResult.Error);
            // Rollback: Put source item back
            await _mediator.Send(new PlaceItemAtPositionCommand(sourceActorId, sourceItemId, sourcePos));
            return;
        }

        // STEP 2: Place both items at swapped positions
        var placeSourceCmd = new PlaceItemAtPositionCommand(targetActorId, sourceItemId, targetPos);
        var placeTargetCmd = new PlaceItemAtPositionCommand(sourceActorId, targetItemId, sourcePos);

        var placeSourceResult = await _mediator.Send(placeSourceCmd);
        if (placeSourceResult.IsFailure)
        {
            _logger.LogError("Swap failed: Could not place source item at target: {Error}", placeSourceResult.Error);
            // Rollback: Put both items back at original positions
            await _mediator.Send(new PlaceItemAtPositionCommand(sourceActorId, sourceItemId, sourcePos));
            await _mediator.Send(new PlaceItemAtPositionCommand(targetActorId, targetItemId, targetPos));
            EmitSignal(SignalName.InventoryChanged); // Refresh to show rollback
            return;
        }

        var placeTargetResult = await _mediator.Send(placeTargetCmd);
        if (placeTargetResult.IsFailure)
        {
            _logger.LogError("Swap failed: Could not place target item at source: {Error}", placeTargetResult.Error);
            // Rollback: Remove source item from wrong place, put both back
            await _mediator.Send(new RemoveItemCommand(targetActorId, sourceItemId));
            await _mediator.Send(new PlaceItemAtPositionCommand(sourceActorId, sourceItemId, sourcePos));
            await _mediator.Send(new PlaceItemAtPositionCommand(targetActorId, targetItemId, targetPos));
            EmitSignal(SignalName.InventoryChanged); // Refresh to show rollback
            return;
        }

        _logger.LogInformation("Swap completed successfully");
        EmitSignal(SignalName.InventoryChanged);
    }

    private async void MoveItemAsync(ActorId sourceActorId, ItemId itemId, GridPosition targetPos)
    {
        if (OwnerActorId == null)
            return;

        var command = new MoveItemBetweenContainersCommand(
            sourceActorId,
            OwnerActorId.Value,
            itemId,
            targetPos);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to move item: {Error}", result.Error);
            return;
        }

        _logger.LogInformation("Item moved to {ContainerTitle} at ({X}, {Y})",
            ContainerTitle, targetPos.X, targetPos.Y);

        // Emit signal to notify parent controller
        // WHY: Cross-container moves affect both source and target inventories
        EmitSignal(SignalName.InventoryChanged);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PUBLIC API (for external refresh triggers)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Reloads inventory data and refreshes display.
    /// WHY: Called by parent controller after cross-container moves to sync all displays.
    /// </summary>
    public void RefreshDisplay()
    {
        LoadInventoryAsync();
    }

    /// <summary>
    /// Checks if this container can accept the given item type based on ContainerType filter.
    /// </summary>
    private bool CanAcceptItemType(string itemType)
    {
        // WHY: Type filtering prevents invalid placements (e.g., potion in weapon slot)
        if (_containerType == ContainerType.WeaponOnly)
        {
            return itemType == "weapon";
        }

        // General containers accept all types
        return true;
    }

    /// <summary>
    /// Queries item type from repository (used when item not in cache).
    /// </summary>
    private async Task<Result<string>> GetItemTypeAsync(ItemId itemId)
    {
        var query = new GetItemByIdQuery(itemId);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return Result.Failure<string>(result.Error);

        return Result.Success(result.Value.Type);
    }
}
