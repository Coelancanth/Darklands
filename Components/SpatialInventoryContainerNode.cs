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
    private Dictionary<GridPosition, ItemId> _itemsAtPositions = new(); // Phase 2: Maps ALL occupied cells → ItemId
    private Dictionary<ItemId, GridPosition> _itemOrigins = new(); // Phase 2: Maps ItemId → origin (from Domain)
    private Dictionary<ItemId, string> _itemTypes = new(); // Cache item types for color coding
    private Dictionary<ItemId, string> _itemNames = new(); // Cache item names for tooltips
    private Dictionary<ItemId, (int Width, int Height)> _itemDimensions = new(); // Cache item dimensions (Phase 2)
    private Dictionary<ItemId, Rotation> _itemRotations = new(); // Cache item rotations (Phase 3)

    // PHASE 3: Drag-time rotation state
    private Rotation _currentDragRotation = default(Darklands.Core.Domain.Common.Rotation); // Rotation during active drag
    private bool _isDragging = false; // Track if drag is active
    private ItemId? _draggingItemId = null; // Track which item is being dragged
    private Control? _dragPreviewNode = null; // Custom drag preview that we can update
    private TextureRect? _dragPreviewSprite = null; // Sprite inside preview for rotation updates

    // TileSet atlas coordinates for drag highlight sprites
    private static readonly Vector2I HIGHLIGHT_GREEN_COORDS = new(1, 6);
    private static readonly Vector2I HIGHLIGHT_RED_COORDS = new(1, 7);

    // UI nodes
    private Label? _titleLabel;
    private GridContainer? _gridContainer;
    private Control? _itemOverlayContainer; // Container for multi-cell item sprites (Phase 2)
    private Control? _highlightOverlayContainer; // Container for drag highlight sprites (Phase 2.4)

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

    public override void _Notification(int what)
    {
        base._Notification(what);

        // Phase 2.4: Clear highlights when mouse exits container during drag
        // WHY: Prevents stale highlights from remaining on source container when dragging to target
        if (what == NotificationMouseExit)
        {
            ClearDragHighlights();
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // Phase 2.4: Clear highlights when drag ends (mouse released)
        // WHY: If drop was rejected, _DropData isn't called, so highlights linger
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed)
            {
                // Left mouse button released - drag ended (successful or rejected)
                ClearDragHighlights();
                _isDragging = false; // Reset drag state
                _draggingItemId = null;
            }

            // PHASE 3: Mouse scroll during drag to rotate item
            // WHY: Rotate while dragging (Tetris/Diablo UX pattern)
            // CRITICAL: Only handle when Pressed == true to avoid double-firing
            if (_isDragging && mouseButton.Pressed &&
                (mouseButton.ButtonIndex == MouseButton.WheelDown || mouseButton.ButtonIndex == MouseButton.WheelUp))
            {
                // Calculate new rotation (scroll DOWN = clockwise, scroll UP = counter-clockwise)
                var newRotation = mouseButton.ButtonIndex == MouseButton.WheelDown
                    ? RotationHelper.RotateClockwise(_currentDragRotation)
                    : RotationHelper.RotateCounterClockwise(_currentDragRotation);

                _logger.LogInformation("🔄 Rotating drag preview: {OldRotation} → {NewRotation} (scroll {Direction})",
                    _currentDragRotation, newRotation,
                    mouseButton.ButtonIndex == MouseButton.WheelDown ? "DOWN" : "UP");

                _currentDragRotation = newRotation;

                // Update sprite preview rotation (size stays constant, only rotation changes)
                if (_dragPreviewSprite != null)
                {
                    // JUST update rotation - size and pivot stay the same (rotate around base center)
                    _dragPreviewSprite.Rotation = RotationHelper.ToRadians(_currentDragRotation);
                }

                // Consume the event to prevent scrolling the container
                GetViewport().SetInputAsHandled();
            }
        }
    }

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

        // Phase 2: Check if ANY cell of a multi-cell item was clicked
        if (!_itemsAtPositions.TryGetValue(gridPos.Value, out var itemId))
        {
            _logger.LogDebug("No item at grid position ({GridX}, {GridY})", gridPos.Value.X, gridPos.Value.Y);
            return default; // No item at this position
        }

        // Get item origin position (top-left) for drag data
        // WHY: Commands operate on origin positions, not clicked cell positions
        var origin = _itemOrigins[itemId];

        // PHASE 3: Initialize drag rotation from item's current rotation
        _currentDragRotation = _itemRotations.TryGetValue(itemId, out var currentRot)
            ? currentRot
            : default(Darklands.Core.Domain.Common.Rotation);
        _isDragging = true;
        _draggingItemId = itemId;

        _logger.LogInformation("🎯 Starting drag: Item {ItemId} from {Container} at origin ({X}, {Y}) with rotation {Rotation}",
            itemId, ContainerTitle, origin.X, origin.Y, _currentDragRotation);

        // PHASE 3: Create sprite-based drag preview (updates with rotation)
        CreateDragPreview(itemId);

        SetDragPreview(_dragPreviewNode!);

        // Return drag data using Guids (simpler than value objects)
        // WHY: Use origin position for commands (not clicked cell)
        var dragData = new Godot.Collections.Dictionary
        {
            ["itemIdGuid"] = itemId.Value.ToString(),
            ["sourceActorIdGuid"] = OwnerActorId?.Value.ToString() ?? string.Empty,
            ["sourceX"] = origin.X,
            ["sourceY"] = origin.Y
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
            ClearDragHighlights(); // Phase 2.4
            return false;
        }

        var dragData = data.AsGodotDictionary();
        if (!dragData.ContainsKey("itemIdGuid"))
        {
            _logger.LogDebug("Drag data missing itemIdGuid key");
            ClearDragHighlights(); // Phase 2.4
            return false;
        }

        // Find target grid position
        var targetPos = PixelToGridPosition(atPosition);
        if (targetPos == null)
        {
            _logger.LogDebug("Target position outside grid bounds");
            ClearDragHighlights(); // Phase 2.4
            return false;
        }

        // Get item ID and dimensions for multi-cell validation (Phase 2.4)
        var itemIdGuidStr = dragData["itemIdGuid"].AsString();
        if (!Guid.TryParse(itemIdGuidStr, out var itemIdGuid))
        {
            ClearDragHighlights();
            return false;
        }

        var itemId = new ItemId(itemIdGuid);

        // Get BASE dimensions - check cache first, query if not found (cross-container drag)
        int baseItemWidth, baseItemHeight;
        if (_itemDimensions.TryGetValue(itemId, out var cachedDimensions))
        {
            (baseItemWidth, baseItemHeight) = cachedDimensions;
        }
        else
        {
            // Cross-container drag: Item not in this container's cache yet
            // Query dimensions from Item repository
            var itemQuery = new GetItemByIdQuery(itemId);
            var itemResult = _mediator.Send(itemQuery).Result; // Blocking, but necessary for validation

            if (itemResult.IsSuccess)
            {
                baseItemWidth = itemResult.Value.InventoryWidth;
                baseItemHeight = itemResult.Value.InventoryHeight;
            }
            else
            {
                // Fallback if query fails
                (baseItemWidth, baseItemHeight) = (1, 1);
            }
        }

        // PHASE 3: Calculate effective dimensions after rotation
        var (itemWidth, itemHeight) = RotationHelper.GetRotatedDimensions(baseItemWidth, baseItemHeight, _currentDragRotation);

        _logger.LogDebug("🔄 Item dimensions: base {BaseW}×{BaseH}, rotated ({Rotation}°) = {W}×{H}",
            baseItemWidth, baseItemHeight, (int)_currentDragRotation, itemWidth, itemHeight);

        // Override dimensions for equipment slots (matches placement handler logic)
        bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;
        if (isEquipmentSlot)
        {
            itemWidth = 1;
            itemHeight = 1;
        }

        // Validation flags
        bool isValid = true;
        string validationError = "";

        // Check bounds
        if (targetPos.Value.X + itemWidth > _gridWidth || targetPos.Value.Y + itemHeight > _gridHeight)
        {
            isValid = false;
            validationError = "exceeds bounds";
        }

        // Check occupation (Phase 2: Check all cells in footprint)
        // Phase 2.4 Fix: Exclude self-collision (item colliding with its current position)
        bool hasCollision = false;
        if (isValid)
        {
            for (int dy = 0; dy < itemHeight && !hasCollision; dy++)
            {
                for (int dx = 0; dx < itemWidth && !hasCollision; dx++)
                {
                    var checkPos = new GridPosition(targetPos.Value.X + dx, targetPos.Value.Y + dy);
                    if (_itemsAtPositions.TryGetValue(checkPos, out var occupyingItemId))
                    {
                        // Phase 2.4: Ignore collision if cell is occupied by the dragged item itself
                        // WHY: Dropping item at same position should be allowed (green highlight, not red)
                        if (occupyingItemId != itemId)
                        {
                            hasCollision = true;
                        }
                    }
                }
            }

            if (hasCollision)
            {
                // Equipment slots allow swap, backpacks don't
                if (!isEquipmentSlot)
                {
                    isValid = false;
                    validationError = "occupied";
                }
                // If equipment slot, allow (swap will happen in _DropData)
            }
        }

        // Type validation for specialized containers (prevent data loss bug)
        if (isValid && OwnerActorId != null)
        {
            // Query item type (synchronous lookup from cache)
            if (_itemTypes.TryGetValue(itemId, out var itemType))
            {
                // Check type compatibility with this container
                if (!CanAcceptItemType(itemType))
                {
                    isValid = false;
                    validationError = "wrong type";
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
                        isValid = false;
                        validationError = "wrong type";
                    }
                }
            }
        }

        // Phase 2.4: Render green/red highlight showing item footprint
        RenderDragHighlight(targetPos.Value, itemWidth, itemHeight, isValid);

        _logger.LogDebug("Can drop at ({X}, {Y}): {IsValid} ({Reason})",
            targetPos.Value.X, targetPos.Value.Y, isValid, isValid ? "valid" : validationError);

        return isValid;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        _logger.LogInformation("_DropData called at position ({X}, {Y}) with rotation {Rotation}",
            atPosition.X, atPosition.Y, _currentDragRotation);

        // Phase 2.4: Clear highlights after drop (will be refreshed after move completes)
        ClearDragHighlights();

        // PHASE 3: Capture rotation before resetting drag state
        var dropRotation = _currentDragRotation;

        // Reset drag state
        _isDragging = false;
        _draggingItemId = null;
        _dragPreviewNode = null;
        _dragPreviewSprite = null;

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

            _logger.LogInformation("Initiating safe swap: {ItemA} ↔ {ItemB} with rotation {Rotation}",
                itemId, targetItemId, dropRotation);

            SwapItemsSafeAsync(sourceActorId, itemId, sourcePos, OwnerActorId!.Value, targetItemId, targetPos.Value, dropRotation);
        }
        else
        {
            // REGULAR MOVE: Position is free
            _logger.LogInformation("Drop confirmed: Moving item {ItemId} to ({X}, {Y}) with rotation {Rotation}",
                itemId, targetPos.Value.X, targetPos.Value.Y, dropRotation);

            MoveItemAsync(sourceActorId, itemId, targetPos.Value, dropRotation);
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

        // Grid container with overlay for multi-cell items
        // WHY: Grid cells provide hit-testing, overlay renders sprites on top
        var gridWrapper = new Control
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        vbox.AddChild(gridWrapper);

        // Background grid (Panel cells for hit-testing)
        _gridContainer = new GridContainer
        {
            CustomMinimumSize = new Vector2(CellSize, CellSize),
            // WHY: Pass allows events to reach Panel cells while also bubbling to parent
            MouseFilter = MouseFilterEnum.Pass
        };
        _gridContainer.AddThemeConstantOverride("h_separation", 2);
        _gridContainer.AddThemeConstantOverride("v_separation", 2);
        gridWrapper.AddChild(_gridContainer);

        // Overlay container for multi-cell item sprites (Phase 2)
        // Highlight overlay container for drag preview (Phase 2.4)
        // WHY: Shows green/red highlights for valid/invalid placement during drag
        // PHASE 3: Render BELOW items (ZIndex=10) so sprites appear above glow
        _highlightOverlayContainer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore, // Let grid cells handle input
            ZIndex = 10 // Render as background glow (below items)
        };
        gridWrapper.AddChild(_highlightOverlayContainer);

        // WHY: Items rendered on separate layer so they can span multiple cells freely
        // PHASE 3: Render ABOVE highlights (ZIndex=15) so sprites are visible
        _itemOverlayContainer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore, // Let grid cells handle input
            ZIndex = 15 // Render above highlights (items on top!)
        };
        gridWrapper.AddChild(_itemOverlayContainer);
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

        // Populate items (Phase 2: Use dimensions from Domain; Phase 3: Use rotations from Domain)
        _itemsAtPositions.Clear();
        _itemOrigins.Clear();
        _itemTypes.Clear();
        _itemNames.Clear();
        _itemDimensions.Clear();
        _itemRotations.Clear(); // Phase 3

        // STEP 1: Store origins, dimensions, and rotations from Domain
        foreach (var (itemId, origin) in inventory.ItemPlacements)
        {
            _itemOrigins[itemId] = origin;

            // Get dimensions from Domain (source of truth)
            var (width, height) = inventory.ItemDimensions.GetValueOrDefault(itemId, (1, 1));
            _itemDimensions[itemId] = (width, height); // Cache for rendering

            // Get rotation from Domain (Phase 3)
            var rotation = inventory.ItemRotations.TryGetValue(itemId, out var rot1) ? rot1 : default(Darklands.Core.Domain.Common.Rotation);
            _itemRotations[itemId] = rotation; // Cache for rendering

            _logger.LogInformation("DEBUG: Item {ItemId} - Domain dimensions: {Width}×{Height}, rotation: {Rotation}",
                itemId, width, height, rotation);
        }

        // STEP 2: Load item metadata (types, names) - needs item IDs from origins
        await LoadItemTypes();

        // STEP 3: Build reverse lookup: ALL occupied cells → ItemId (multi-cell support)
        foreach (var (itemId, origin) in _itemOrigins)
        {
            var (baseWidth, baseHeight) = _itemDimensions[itemId]; // Base dimensions from Domain
            var rotation = _itemRotations[itemId]; // Rotation from Domain

            // PHASE 3: Calculate effective dimensions after rotation
            var (effectiveWidth, effectiveHeight) = RotationHelper.GetRotatedDimensions(baseWidth, baseHeight, rotation);

            // Reserve ALL cells occupied by this item (using rotated dimensions)
            for (int dy = 0; dy < effectiveHeight; dy++)
            {
                for (int dx = 0; dx < effectiveWidth; dx++)
                {
                    var occupiedCell = new GridPosition(origin.X + dx, origin.Y + dy);
                    _itemsAtPositions[occupiedCell] = itemId;
                }
            }

            _logger.LogInformation("Item {ItemId} at ({X},{Y}) occupies {Width}×{Height} cells (base: {BaseWidth}×{BaseHeight}, rotation: {Rotation})",
                itemId, origin.X, origin.Y, effectiveWidth, effectiveHeight, baseWidth, baseHeight, rotation);
        }

        RefreshGridDisplay();
    }

    private async Task LoadItemTypes()
    {
        // Query item details to determine types (weapon, item, etc.) and names (for tooltips)
        // WHY: Dimensions already cached from Domain in LoadInventoryAsync
        foreach (var itemId in _itemOrigins.Keys)
        {
            var query = new GetItemByIdQuery(itemId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _itemTypes[itemId] = result.Value.Type;
                _itemNames[itemId] = result.Value.Name;

                _logger.LogDebug("Item {ItemId}: {Name} ({Type}) Sprite {SpriteW}×{SpriteH}, Inventory {InvW}×{InvH}",
                    itemId, result.Value.Name, result.Value.Type,
                    result.Value.SpriteWidth, result.Value.SpriteHeight,
                    result.Value.InventoryWidth, result.Value.InventoryHeight);
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
        // WHY: Phase 2 - Render multi-cell items with TextureRect sprites
        if (_gridContainer == null || _itemOverlayContainer == null)
            return;

        // STEP 1: Clear previous item sprites from overlay
        ClearAllItemSprites();

        // STEP 2: Update grid cell tooltips (cells remain for hit-testing)
        for (int i = 0; i < _gridContainer.GetChildCount(); i++)
        {
            if (_gridContainer.GetChild(i) is Panel cell)
            {
                int x = i % _gridWidth;
                int y = i / _gridWidth;
                var gridPos = new GridPosition(x, y);

                // Update tooltip (show which item occupies this cell, if any)
                if (_itemsAtPositions.TryGetValue(gridPos, out var itemId))
                {
                    string itemName = _itemNames.GetValueOrDefault(itemId, "Unknown");
                    string itemType = _itemTypes.GetValueOrDefault(itemId, "unknown");
                    cell.TooltipText = $"{itemName} ({itemType})";
                }
                else
                {
                    cell.TooltipText = $"Empty ({gridPos.X}, {gridPos.Y})";
                }
            }
        }

        // STEP 3: Render each item sprite ONCE at its origin position
        foreach (var (itemId, origin) in _itemOrigins)
        {
            RenderMultiCellItemSprite(itemId, origin);
        }

        _logger.LogDebug("{ContainerTitle}: {ItemCount} items displayed",
            ContainerTitle, _itemOrigins.Count);
    }

    /// <summary>
    /// Clears all item sprites from the overlay container.
    /// WHY: Called before re-rendering to avoid duplicate sprites.
    /// </summary>
    private void ClearAllItemSprites()
    {
        if (_itemOverlayContainer == null)
            return;

        foreach (Node child in _itemOverlayContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    /// <summary>
    /// Renders a multi-cell item sprite using TextureRect (Phase 2).
    /// WHY: Sprite size (visual) ≠ Inventory size (logical occupation).
    /// </summary>
    /// <param name="itemId">Item to render</param>
    /// <param name="origin">Top-left grid position of the item</param>
    private async void RenderMultiCellItemSprite(ItemId itemId, GridPosition origin)
    {
        // Query item data for atlas coordinates and dimensions
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = await _mediator.Send(itemQuery);

        if (itemResult.IsFailure)
        {
            _logger.LogWarning("Failed to query item {ItemId} for rendering: {Error}",
                itemId, itemResult.Error);
            return;
        }

        var item = itemResult.Value;

        // Get INVENTORY dimensions for positioning/sizing (logical occupation)
        // WHY: Item occupies InventoryWidth×InventoryHeight grid cells
        var (baseInvWidth, baseInvHeight) = _itemDimensions.GetValueOrDefault(itemId, (1, 1));

        // PHASE 3: Get rotation for sprite transform
        var rotation = _itemRotations.TryGetValue(itemId, out var rot2) ? rot2 : default(Darklands.Core.Domain.Common.Rotation);
        var (effectiveInvWidth, effectiveInvHeight) = RotationHelper.GetRotatedDimensions(baseInvWidth, baseInvHeight, rotation);

        // PHASE 2: Render TextureRect sprite if TileSet available
        if (_itemTileSet != null)
        {
            var atlasSource = _itemTileSet.GetSource(0) as TileSetAtlasSource;
            if (atlasSource != null)
            {
                // Calculate pixel position based on EFFECTIVE (rotated) dimensions for placement
                // But sprite SIZE is ALWAYS base dimensions (rotation happens visually)
                int separationX = 2;
                int separationY = 2;
                float pixelX = origin.X * (CellSize + separationX);
                float pixelY = origin.Y * (CellSize + separationY);

                // PHASE 3: Sprite size is BASE dimensions (rotation visual only)
                float baseSpriteWidth = baseInvWidth * CellSize + (baseInvWidth - 1) * separationX;
                float baseSpriteHeight = baseInvHeight * CellSize + (baseInvHeight - 1) * separationX;

                // Create AtlasTexture for this specific tile (VS_009 pattern)
                // WHY: Extracts sprite region from atlas (sprite dimensions are SpriteWidth×SpriteHeight)
                var tileCoords = new Vector2I(item.AtlasX, item.AtlasY);
                var region = atlasSource.GetTileTextureRegion(tileCoords);

                var atlasTexture = new AtlasTexture
                {
                    Atlas = atlasSource.Texture,
                    Region = region
                };

                var textureRect = new TextureRect
                {
                    Name = $"Item_{itemId.Value}",
                    Texture = atlasTexture, // Use AtlasTexture wrapper
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest, // Pixel-perfect rendering
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight), // BASE size (not rotated)
                    Size = new Vector2(baseSpriteWidth, baseSpriteHeight),
                    Position = new Vector2(pixelX, pixelY),
                    MouseFilter = MouseFilterEnum.Ignore, // Grid cells handle input
                    // PHASE 3: Apply rotation transform (rotate around BASE center)
                    Rotation = RotationHelper.ToRadians(rotation),
                    PivotOffset = new Vector2(baseSpriteWidth / 2f, baseSpriteHeight / 2f), // Rotate around BASE center
                    ZIndex = 1 // PHASE 3: Render ABOVE highlights (positive = foreground)
                };

                _itemOverlayContainer?.AddChild(textureRect);

                _logger.LogDebug("Rendered {ItemName} at ({X},{Y}): Sprite {SpriteW}×{SpriteH}, Inventory {InvW}×{InvH}, Rotation {Rotation}°",
                    item.Name, origin.X, origin.Y,
                    item.SpriteWidth, item.SpriteHeight,
                    effectiveInvWidth, effectiveInvHeight,
                    (int)rotation);
            }
        }
        else
        {
            // FALLBACK: Phase 1 ColorRect rendering (no TileSet assigned)
            // Note: ColorRect doesn't support rotation, so fallback doesn't show rotation visually
            var colorRect = new ColorRect
            {
                Name = $"Item_{itemId.Value}_Fallback",
                Color = GetItemColorFallback(item.Name),
                CustomMinimumSize = new Vector2(baseInvWidth * CellSize * 0.9f, baseInvHeight * CellSize * 0.9f),
                Position = new Vector2(origin.X * CellSize + CellSize * 0.05f, origin.Y * CellSize + CellSize * 0.05f),
                MouseFilter = MouseFilterEnum.Ignore,
                Rotation = RotationHelper.ToRadians(rotation),
                PivotOffset = new Vector2(baseInvWidth * CellSize * 0.45f, baseInvHeight * CellSize * 0.45f)
            };

            _itemOverlayContainer?.AddChild(colorRect);
        }
    }

    /// <summary>
    /// Fallback color coding when TileSet not available (Phase 1 compatibility).
    /// </summary>
    private Color GetItemColorFallback(string itemName)
    {
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
    /// Renders green/red highlight sprites showing where multi-cell item would be placed (Phase 2.4).
    /// </summary>
    /// <param name="origin">Top-left position where item would be placed</param>
    /// <param name="width">Item width in cells</param>
    /// <param name="height">Item height in cells</param>
    /// <param name="isValid">True for green (valid placement), false for red (invalid)</param>
    private void RenderDragHighlight(GridPosition origin, int width, int height, bool isValid)
    {
        if (_highlightOverlayContainer == null || _itemTileSet == null)
            return;

        // Clear previous highlights
        ClearDragHighlights();

        var atlasSource = _itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
            return;

        // Get highlight tile coords from TileSet
        var highlightCoords = isValid ? HIGHLIGHT_GREEN_COORDS : HIGHLIGHT_RED_COORDS;
        var region = atlasSource.GetTileTextureRegion(highlightCoords);

        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        // Render highlight sprite for each cell in item footprint
        int separationX = 2;
        int separationY = 2;

        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                float pixelX = (origin.X + dx) * (CellSize + separationX);
                float pixelY = (origin.Y + dy) * (CellSize + separationY);

                var highlight = new TextureRect
                {
                    Name = $"Highlight_{origin.X + dx}_{origin.Y + dy}",
                    Texture = atlasTexture,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    CustomMinimumSize = new Vector2(CellSize, CellSize),
                    Position = new Vector2(pixelX, pixelY),
                    MouseFilter = MouseFilterEnum.Ignore,
                    Modulate = new Color(1, 1, 1, 0.7f), // Semi-transparent
                    ZIndex = -1 // PHASE 3: Render BEHIND items (negative = background)
                };

                _highlightOverlayContainer.AddChild(highlight);
            }
        }
    }

    /// <summary>
    /// Clears all drag highlight sprites from overlay.
    /// </summary>
    private void ClearDragHighlights()
    {
        if (_highlightOverlayContainer == null)
            return;

        // PHASE 3: Use Free() instead of QueueFree() for immediate removal
        // WHY: QueueFree() delays removal until end of frame, causing ghost highlights
        foreach (Node child in _highlightOverlayContainer.GetChildren())
        {
            child.Free(); // Immediate removal, not queued
        }
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
        GridPosition targetPos,
        Rotation rotation) // PHASE 3: Rotation for dragged item
    {
        _logger.LogInformation("SAFE SWAP: {SourceItem} @ ({SourceX},{SourceY}) ↔ {TargetItem} @ ({TargetX},{TargetY}) with rotation {Rotation}",
            sourceItemId, sourcePos.X, sourcePos.Y,
            targetItemId, targetPos.X, targetPos.Y, rotation);

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

    private async void MoveItemAsync(ActorId sourceActorId, ItemId itemId, GridPosition targetPos, Rotation rotation)
    {
        if (OwnerActorId == null)
            return;

        var command = new MoveItemBetweenContainersCommand(
            sourceActorId,
            OwnerActorId.Value,
            itemId,
            targetPos,
            rotation); // PHASE 3: Pass rotation from drag-drop

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

    /// <summary>
    /// Creates a sprite-based drag preview that can be updated with rotation (Phase 3).
    /// </summary>
    private async void CreateDragPreview(ItemId itemId)
    {
        // Query item data for sprite rendering
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = await _mediator.Send(itemQuery);

        if (itemResult.IsFailure || _itemTileSet == null)
        {
            // Fallback: Create simple label preview
            _dragPreviewNode = new Label { Text = $"📦 {_itemNames.GetValueOrDefault(itemId, "Item")}" };
            return;
        }

        var item = itemResult.Value;
        var atlasSource = _itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
        {
            _dragPreviewNode = new Label { Text = $"📦 {item.Name}" };
            return;
        }

        // Get base dimensions (UNROTATED)
        var (baseWidth, baseHeight) = _itemDimensions.GetValueOrDefault(itemId, (1, 1));

        // Calculate BASE sprite size (before rotation)
        float baseSpriteWidth = baseWidth * CellSize;
        float baseSpriteHeight = baseHeight * CellSize;

        // Extract sprite from atlas
        var tileCoords = new Vector2I(item.AtlasX, item.AtlasY);
        var region = atlasSource.GetTileTextureRegion(tileCoords);
        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        // Create sprite preview (size is ALWAYS base dimensions, rotation happens around center)
        _dragPreviewSprite = new TextureRect
        {
            Texture = atlasTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Size = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Rotation = RotationHelper.ToRadians(_currentDragRotation),
            PivotOffset = new Vector2(baseSpriteWidth / 2f, baseSpriteHeight / 2f), // Rotate around BASE center
            Modulate = new Color(1, 1, 1, 0.8f) // Semi-transparent
        };

        // Wrap in container (container size is base dimensions, sprite rotates inside)
        _dragPreviewNode = new Control
        {
            CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight)
        };
        _dragPreviewNode.AddChild(_dragPreviewSprite);
    }

    /// <summary>
    /// Rotates an item in inventory (Phase 3).
    /// Sends RotateItemCommand and refreshes display on success.
    /// </summary>
    private async void RotateItemAsync(ItemId itemId, Rotation newRotation)
    {
        if (OwnerActorId == null)
            return;

        var command = new RotateItemCommand(OwnerActorId.Value, itemId, newRotation);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to rotate item {ItemId}: {Error}", itemId, result.Error);
            return;
        }

        _logger.LogInformation("Successfully rotated item {ItemId} to {Rotation}", itemId, newRotation);

        // Refresh display to show rotated sprite
        RefreshDisplay();
    }
}
