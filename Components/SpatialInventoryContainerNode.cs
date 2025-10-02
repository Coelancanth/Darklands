using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
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
    // DEPENDENCIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IMediator _mediator = null!;
    private TileSet? _itemTileSet;

    // Grid state
    private int _gridWidth;
    private int _gridHeight;
    private Dictionary<GridPosition, ItemId> _itemsAtPositions = new();

    // UI nodes
    private Label? _titleLabel;
    private GridContainer? _gridContainer;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Get dependencies from parent controller
        var parent = GetParent();
        if (parent is Darklands.SpatialInventoryTestController controller)
        {
            _mediator = controller.GetMediator();
            _itemTileSet = controller.GetItemTileSet();
        }
        else
        {
            GD.PrintErr("[SpatialInventoryContainerNode] Parent must be SpatialInventoryTestController");
            return;
        }

        if (OwnerActorId == null)
        {
            GD.PrintErr("[SpatialInventoryContainerNode] OwnerActorId not assigned");
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
        // Find which grid cell was clicked
        var gridPos = PixelToGridPosition(atPosition);
        if (gridPos == null || !_itemsAtPositions.ContainsKey(gridPos.Value))
            return default; // No item at this position

        var itemId = _itemsAtPositions[gridPos.Value];

        // Create drag preview (visual feedback)
        var preview = new Label
        {
            Text = "📦 Item",
            Modulate = new Color(1, 1, 1, 0.7f)
        };
        SetDragPreview(preview);

        // Return drag data using Guids (simpler than value objects)
        var dragData = new Godot.Collections.Dictionary
        {
            ["itemIdGuid"] = itemId.Value.ToString(),
            ["sourceActorIdGuid"] = OwnerActorId?.Value.ToString() ?? string.Empty,
            ["sourceX"] = gridPos.Value.X,
            ["sourceY"] = gridPos.Value.Y
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

        // Find target grid position
        var targetPos = PixelToGridPosition(atPosition);
        if (targetPos == null)
            return false;

        // Check if position is free
        return !_itemsAtPositions.ContainsKey(targetPos.Value);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dragData = data.AsGodotDictionary();
        var itemIdGuidStr = dragData["itemIdGuid"].AsString();
        var sourceActorIdGuidStr = dragData["sourceActorIdGuid"].AsString();

        if (!Guid.TryParse(itemIdGuidStr, out var itemIdGuid) ||
            !Guid.TryParse(sourceActorIdGuidStr, out var sourceActorIdGuid))
        {
            GD.PrintErr("Failed to parse drag data GUIDs");
            return;
        }

        var targetPos = PixelToGridPosition(atPosition);
        if (targetPos == null)
            return;

        // Reconstruct value objects from Guids
        var itemId = new ItemId(itemIdGuid);
        var sourceActorId = new ActorId(sourceActorIdGuid);

        // Send MoveItemBetweenContainersCommand
        MoveItemAsync(sourceActorId, itemId, targetPos.Value);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void BuildUI()
    {
        var vbox = new VBoxContainer();
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
            CustomMinimumSize = new Vector2(CellSize, CellSize)
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
            GD.PrintErr($"Failed to load inventory: {result.Error}");
            return;
        }

        var inventory = result.Value;
        _gridWidth = inventory.GridWidth;
        _gridHeight = inventory.GridHeight;

        // Update title with capacity info
        if (_titleLabel != null)
        {
            _titleLabel.Text = $"{ContainerTitle} ({inventory.Count}/{inventory.Capacity})";
        }

        // Build grid cells
        if (_gridContainer != null)
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
        }

        // Populate items
        _itemsAtPositions.Clear();
        foreach (var placement in inventory.ItemPlacements)
        {
            _itemsAtPositions[placement.Value] = placement.Key;
        }

        RefreshGridDisplay();
    }

    private Control CreateGridCell(GridPosition pos)
    {
        var cell = new Panel
        {
            CustomMinimumSize = new Vector2(CellSize, CellSize),
            TooltipText = $"Grid ({pos.X}, {pos.Y})"
        };

        // Add visual styling (gray border for empty cells)
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
        // Update grid cells to show items
        // (Simplified for Phase 1 - just show occupied/empty)
        GD.Print($"[SpatialInventoryContainerNode] {ContainerTitle} loaded: {_itemsAtPositions.Count} items in {_gridWidth}×{_gridHeight} grid");
    }

    private GridPosition? PixelToGridPosition(Vector2 pixelPos)
    {
        if (_gridContainer == null)
            return null;

        // Convert pixel position to grid coordinates
        int x = (int)(pixelPos.X / CellSize);
        int y = (int)(pixelPos.Y / CellSize);

        if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
            return null;

        return new GridPosition(x, y);
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
            GD.PrintErr($"Failed to move item: {result.Error}");
            return;
        }

        GD.Print($"✅ Item moved to {ContainerTitle} at ({targetPos.X}, {targetPos.Y})");

        // Reload inventory to reflect changes
        LoadInventoryAsync();
    }
}
