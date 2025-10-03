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
    private Dictionary<ItemId, ItemShape> _itemShapes = new(); // Phase 4: Cache item shapes for L-shape rendering
    private Dictionary<ItemId, Rotation> _itemRotations = new(); // Cache item rotations (Phase 3)
    private Dictionary<ItemId, Node> _itemSpriteNodes = new(); // PHASE 3: Direct references to sprite nodes for hiding during drag

    // PHASE 3: Drag-time rotation state (SHARED across all container instances for cross-container drag support)
    // WHY: Godot's drag data is immutable after _GetDragData, but scroll wheel rotates AFTER drag starts
    // Solution: Static variable allows target container to read latest rotation from source container
    private static Rotation _sharedDragRotation = default(Darklands.Core.Domain.Common.Rotation);
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

                // PHASE 3: If drag was active but cancelled (not dropped), restore hidden sprite
                // WHY: HideItemSprite() removed the node, need full refresh to recreate it
                bool wasDragging = _isDragging && _draggingItemId != null;

                _isDragging = false; // Reset drag state
                _draggingItemId = null;

                // If drag was cancelled (not dropped), refresh to restore sprite
                if (wasDragging)
                {
                    _logger.LogInformation("🔄 Drag cancelled, refreshing to restore hidden sprite");
                    LoadInventoryAsync(); // Full reload to recreate sprite nodes
                }
            }

            // PHASE 3: Mouse scroll during drag to rotate item
            // WHY: Rotate while dragging (Tetris/Diablo UX pattern)
            // CRITICAL: Only handle when Pressed == true to avoid double-firing
            if (_isDragging && mouseButton.Pressed &&
                (mouseButton.ButtonIndex == MouseButton.WheelDown || mouseButton.ButtonIndex == MouseButton.WheelUp))
            {
                // Calculate new rotation (scroll DOWN = clockwise, scroll UP = counter-clockwise)
                var newRotation = mouseButton.ButtonIndex == MouseButton.WheelDown
                    ? RotationHelper.RotateClockwise(_sharedDragRotation)
                    : RotationHelper.RotateCounterClockwise(_sharedDragRotation);

                _logger.LogInformation("🔄 ROTATION: {OldRotation} → {NewRotation} (scroll {Direction})",
                    _sharedDragRotation, newRotation,
                    mouseButton.ButtonIndex == MouseButton.WheelDown ? "DOWN" : "UP");

                _sharedDragRotation = newRotation;

                _logger.LogInformation("✅ ROTATION STATE: _sharedDragRotation = {SharedRotation}, _dragPreviewSprite exists: {PreviewExists}",
                    _sharedDragRotation, _dragPreviewSprite != null);

                // PHASE 3 FIX: Update sprite preview - ONLY rotation changes (single-layer approach)
                // WHY: Container is base-sized, texture just rotates inside via PivotOffset
                if (_dragPreviewSprite != null)
                {
                    // Simply update rotation - size and position stay constant!
                    var radians = RotationHelper.ToRadians(_sharedDragRotation);
                    _dragPreviewSprite.Rotation = radians;

                    _logger.LogInformation("🎭 DRAG PREVIEW updated: rotation = {Rotation} ({Radians} rad)",
                        _sharedDragRotation, radians);

                    // No need to update container size or texture position
                    // Container stays BASE size, texture rotates around its PivotOffset
                }
                else
                {
                    _logger.LogWarning("❌ DRAG PREVIEW is null - cannot update rotation!");
                }

                // PHASE 3 BUG FIX: Update highlights immediately after rotation
                // WHY: _CanDropData only called on mouse move, not on scroll
                // SOLUTION: Force Godot to re-evaluate drop validation by simulating a micro mouse movement
                // This triggers _CanDropData on whichever container the mouse is ACTUALLY over
                var viewport = GetViewport();
                var currentMousePos = viewport.GetMousePosition();

                // Simulate tiny mouse movement to trigger _CanDropData on the container under the cursor
                // WHY: Moving by 0.1 pixels is imperceptible but forces Godot to re-check drop targets
                Input.WarpMouse(currentMousePos + new Vector2(0.1f, 0));
                Input.WarpMouse(currentMousePos); // Restore original position

                _logger.LogInformation("🔄 Forced highlight refresh after rotation to {Rotation}", _sharedDragRotation);

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

        // PHASE 3: Initialize SHARED drag rotation from item's current rotation
        // WHY: Static variable allows target container to read rotation during cross-container drag
        _sharedDragRotation = _itemRotations.TryGetValue(itemId, out var currentRot)
            ? currentRot
            : default(Darklands.Core.Domain.Common.Rotation);
        _isDragging = true;
        _draggingItemId = itemId;

        _logger.LogInformation("🎯 Starting drag: Item {ItemId} from {Container} at origin ({X}, {Y}) with rotation {Rotation}",
            itemId, ContainerTitle, origin.X, origin.Y, _sharedDragRotation);

        // PHASE 3: Immediately hide source sprite (remove from overlay)
        // WHY: Uses direct node reference - no string matching needed!
        HideItemSprite(itemId);

        // PHASE 3: Create sprite-based drag preview (updates with rotation) and set immediately
        CreateDragPreview(itemId);
        if (_dragPreviewNode != null)
        {
            SetDragPreview(_dragPreviewNode);
        }

        // Return drag data using Guids (simpler than value objects)
        // WHY: Use origin position for commands (not clicked cell)
        // NOTE: Rotation is stored in static _sharedDragRotation and read by target container
        // (drag data is immutable, but rotation can change via scroll wheel after drag starts)
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

        // PHASE 4: Delegate ALL validation to Core (no business logic in Presentation!)
        // WHY: Single source of truth - Core owns collision detection with L-shape support
        if (OwnerActorId == null)
        {
            ClearDragHighlights();
            return false;
        }

        // Read rotation from SHARED static variable (cross-container safe)
        var dragRotation = _sharedDragRotation;

        // Delegate to Core: Validates bounds, collision (with L-shapes!), type compatibility
        var canPlaceQuery = new CanPlaceItemAtQuery(
            OwnerActorId.Value,
            itemId,
            targetPos.Value,
            dragRotation);

        var validationResult = _mediator.Send(canPlaceQuery).Result; // Blocking OK for UI validation

        bool isValid = validationResult.IsSuccess && validationResult.Value;
        string validationError = isValid ? "" : "validation failed";

        // Phase 4: Render green/red highlight showing item footprint (L-shape accurate!)
        RenderDragHighlight(targetPos.Value, itemId, dragRotation, isValid);

        _logger.LogDebug("Can drop at ({X}, {Y}): {IsValid} ({Reason})",
            targetPos.Value.X, targetPos.Value.Y, isValid, isValid ? "valid" : validationError);

        return isValid;
    }

    /// <summary>
    /// Updates drag highlights at the given mouse position.
    /// WHY: Called manually after rotation to refresh highlights (Godot doesn't call _CanDropData on scroll).
    /// </summary>
    private void UpdateDragHighlightsAtPosition(Vector2 mousePosition)
    {
        if (!_isDragging || _draggingItemId == null)
        {
            ClearDragHighlights();
            return;
        }

        // Find target grid position
        var targetPos = PixelToGridPosition(mousePosition);
        if (targetPos == null)
        {
            ClearDragHighlights();
            return;
        }

        var itemId = _draggingItemId.Value;

        // PHASE 4: Delegate ALL validation to Core (no business logic in Presentation!)
        if (OwnerActorId == null)
        {
            ClearDragHighlights();
            return;
        }

        // Delegate to Core: Validates bounds, collision (with L-shapes!), type compatibility
        var canPlaceQuery = new CanPlaceItemAtQuery(
            OwnerActorId.Value,
            itemId,
            targetPos.Value,
            _sharedDragRotation);

        var validationResult = _mediator.Send(canPlaceQuery).Result; // Blocking OK for manual highlight update

        bool isValid = validationResult.IsSuccess && validationResult.Value;

        // Phase 4: Render highlight with actual shape (L-shape accurate!)
        RenderDragHighlight(targetPos.Value, itemId, _sharedDragRotation, isValid);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        // PHASE 3: Read rotation from SHARED static variable (cross-container safe)
        // WHY: Drag data is immutable, but rotation changes via scroll wheel AFTER drag starts
        var dropRotation = _sharedDragRotation;

        _logger.LogInformation("_DropData called at position ({X}, {Y}) with rotation {Rotation}",
            atPosition.X, atPosition.Y, dropRotation);

        var dragData = data.AsGodotDictionary();

        // Phase 2.4: Clear highlights after drop (will be refreshed after move completes)
        ClearDragHighlights();

        // Reset drag state
        _isDragging = false;
        _draggingItemId = null;
        _dragPreviewNode = null;
        _dragPreviewSprite = null;

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

        // PHASE 3: Overlay containers (z-order solution: extreme transparency)
        // WHY: Godot Control z-ordering is unreliable, CanvasLayer breaks positioning
        // Pragmatic solution: Make highlights SO transparent they don't obscure items

        _highlightOverlayContainer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        gridWrapper.AddChild(_highlightOverlayContainer);

        _itemOverlayContainer = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore
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
        _itemShapes.Clear(); // Phase 4: Clear shape cache
        _itemRotations.Clear(); // Phase 3

        // STEP 1: Store origins, dimensions, and rotations from Domain
        foreach (var (itemId, origin) in inventory.ItemPlacements)
        {
            _itemOrigins[itemId] = origin;

            // Get dimensions from Domain (source of truth)
            var (width, height) = inventory.ItemDimensions.GetValueOrDefault(itemId, (1, 1));
            _itemDimensions[itemId] = (width, height); // Cache for rendering

            // Get shape from Domain (Phase 4: L-shape support)
            if (inventory.ItemShapes.TryGetValue(itemId, out var shape))
            {
                _itemShapes[itemId] = shape; // Cache for accurate highlight rendering
            }

            // Get rotation from Domain (Phase 3)
            var rotation = inventory.ItemRotations.TryGetValue(itemId, out var rot1) ? rot1 : default(Darklands.Core.Domain.Common.Rotation);
            _itemRotations[itemId] = rotation; // Cache for rendering
        }

        // STEP 2: Load item metadata (types, names) - needs item IDs from origins
        await LoadItemTypes();

        // STEP 3: Build reverse lookup: ALL occupied cells → ItemId (multi-cell support)
        foreach (var (itemId, origin) in _itemOrigins)
        {
            var (baseWidth, baseHeight) = _itemDimensions[itemId]; // Base dimensions from Domain
            var rotation = _itemRotations[itemId]; // Rotation from Domain

            // PHASE 4: Use ItemShape.OccupiedCells for accurate L-shape collision
            // CRITICAL: Don't iterate bounding box - that fills empty cells in L-shapes!
            if (_itemShapes.TryGetValue(itemId, out var shape))
            {
                // L-shape support: Rotate shape, then iterate ONLY actual occupied cells
                var rotatedShape = shape;
                for (int i = 0; i < (int)rotation; i++)
                {
                    rotatedShape = rotatedShape.RotateClockwise().Value; // Safe: rotation always succeeds for valid shapes
                }

                foreach (var offset in rotatedShape.OccupiedCells)
                {
                    var occupiedCell = new GridPosition(origin.X + offset.X, origin.Y + offset.Y);
                    _itemsAtPositions[occupiedCell] = itemId;
                }

                // Get item metadata for enhanced logging
                var itemName = _itemNames.GetValueOrDefault(itemId, "Unknown");
                var itemType = _itemTypes.GetValueOrDefault(itemId, "unknown");

                _logger.LogDebug("Item '{ItemName}' ({ItemType}) [{ItemId}] at ({X},{Y}) occupies {OccupiedCount} cells (shape: {ShapeWidth}×{ShapeHeight}, rotation: {Rotation})",
                    itemName, itemType, itemId, origin.X, origin.Y, rotatedShape.OccupiedCells.Count, rotatedShape.Width, rotatedShape.Height, rotation);
            }
            else
            {
                // Fallback for items without shape data (legacy rectangle mode)
                var (effectiveWidth, effectiveHeight) = RotationHelper.GetRotatedDimensions(baseWidth, baseHeight, rotation);

                for (int dy = 0; dy < effectiveHeight; dy++)
                {
                    for (int dx = 0; dx < effectiveWidth; dx++)
                    {
                        var occupiedCell = new GridPosition(origin.X + dx, origin.Y + dy);
                        _itemsAtPositions[occupiedCell] = itemId;
                    }
                }

                // Get item metadata for enhanced logging
                var itemName = _itemNames.GetValueOrDefault(itemId, "Unknown");
                var itemType = _itemTypes.GetValueOrDefault(itemId, "unknown");

                _logger.LogInformation("Item '{ItemName}' ({ItemType}) [{ItemId}] at ({X},{Y}) occupies {Width}×{Height} cells (base: {BaseWidth}×{BaseHeight}, rotation: {Rotation})",
                    itemName, itemType, itemId, origin.X, origin.Y, effectiveWidth, effectiveHeight, baseWidth, baseHeight, rotation);
            }
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

        // PHASE 3: Clear sprite node references
        _itemSpriteNodes.Clear();
    }

    /// <summary>
    /// Hides a specific item sprite immediately (used during drag start).
    /// WHY: Sprite already rendered, need immediate removal (not wait for refresh).
    /// PHASE 3: Uses direct node reference - no string matching, no async issues!
    /// </summary>
    private void HideItemSprite(ItemId itemId)
    {
        // Direct dictionary lookup using ItemId (no string name matching!)
        if (_itemSpriteNodes.TryGetValue(itemId, out var spriteNode))
        {
            _logger.LogInformation("✅ Hiding sprite for item {ItemId} (direct reference)", itemId);
            spriteNode.Free(); // Immediate removal from scene tree
            _itemSpriteNodes.Remove(itemId); // Clear reference
        }
        else
        {
            _logger.LogWarning("❌ Item {ItemId} sprite node not found in cache", itemId);
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
                // Calculate pixel position from origin (Domain already accounts for rotation!)
                int separationX = 2;
                int separationY = 2;
                float pixelX = origin.X * (CellSize + separationX);
                float pixelY = origin.Y * (CellSize + separationY);

                // EQUIPMENT SLOT FIX: Center items in 1×1 equipment slots (Diablo 2 pattern)
                // WHY: Equipment slots show items centered regardless of size
                bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly && _gridWidth == 1 && _gridHeight == 1;
                if (isEquipmentSlot)
                {
                    // Center item within the single cell (96px)
                    // Available space: CellSize × CellSize
                    // Item sprite: effectiveSpriteWidth × effectiveSpriteHeight (after rotation)
                    var (effectiveWidth, effectiveHeight) = RotationHelper.GetRotatedDimensions(baseInvWidth, baseInvHeight, rotation);
                    float effectiveW = effectiveWidth * CellSize;
                    float effectiveH = effectiveHeight * CellSize;

                    // Center horizontally and vertically
                    pixelX = (CellSize - effectiveW) / 2f;
                    pixelY = (CellSize - effectiveH) / 2f;

                    _logger.LogDebug("Equipment slot centering: item {ItemId} centered at ({X},{Y}), size {W}×{H}",
                        itemId, pixelX, pixelY, effectiveW, effectiveH);
                }

                // PHASE 3 FIX: Calculate sizes for BOTH base and effective dimensions
                // WHY: Container size = effective dimensions (what cells it occupies)
                //      Texture size = base dimensions (preserves aspect ratio before rotation)
                float baseSpriteWidth = baseInvWidth * CellSize + (baseInvWidth - 1) * separationX;
                float baseSpriteHeight = baseInvHeight * CellSize + (baseInvHeight - 1) * separationY;
                float effectiveSpriteWidth = effectiveInvWidth * CellSize + (effectiveInvWidth - 1) * separationX;
                float effectiveSpriteHeight = effectiveInvHeight * CellSize + (effectiveInvHeight - 1) * separationY;

                // Create AtlasTexture for this specific tile (VS_009 pattern)
                // WHY: Extracts sprite region from atlas (sprite dimensions are SpriteWidth×SpriteHeight)
                var tileCoords = new Vector2I(item.AtlasX, item.AtlasY);
                var region = atlasSource.GetTileTextureRegion(tileCoords);

                var atlasTexture = new AtlasTexture
                {
                    Atlas = atlasSource.Texture,
                    Region = region
                };

                // PHASE 3 FIX: Create WRAPPER container at effective size, texture inside at base size
                // WHY: Container occupies rotated cells, texture preserves aspect ratio and rotates
                var textureContainer = new Control
                {
                    Name = $"ItemContainer_{itemId.Value}",
                    CustomMinimumSize = new Vector2(effectiveSpriteWidth, effectiveSpriteHeight),
                    Size = new Vector2(effectiveSpriteWidth, effectiveSpriteHeight),
                    Position = new Vector2(pixelX, pixelY),
                    MouseFilter = MouseFilterEnum.Ignore,
                    // Ensure item layer is absolute above highlight layer
                    ZAsRelative = false,
                    ZIndex = 200
                };

                var textureRect = new TextureRect
                {
                    Name = $"Item_{itemId.Value}",
                    Texture = atlasTexture,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    // Texture size is ALWAYS base dimensions (aspect ratio preserved)
                    CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight),
                    Size = new Vector2(baseSpriteWidth, baseSpriteHeight),
                    // Position inside container (rotate around center of BASE dimensions)
                    Position = new Vector2(
                        (effectiveSpriteWidth - baseSpriteWidth) / 2f,
                        (effectiveSpriteHeight - baseSpriteHeight) / 2f
                    ),
                    MouseFilter = MouseFilterEnum.Ignore,
                    // Rotate texture around its own center
                    Rotation = RotationHelper.ToRadians(rotation),
                    PivotOffset = new Vector2(baseSpriteWidth / 2f, baseSpriteHeight / 2f),
                    // Ensure sprites render above highlights
                    ZIndex = 100
                };

                textureContainer.AddChild(textureRect);
                _itemOverlayContainer?.AddChild(textureContainer);

                // PHASE 3: Store CONTAINER reference for direct hiding during drag
                _itemSpriteNodes[itemId] = textureContainer;
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
                PivotOffset = new Vector2(baseInvWidth * CellSize * 0.45f, baseInvHeight * CellSize * 0.45f),
                ZIndex = 100
            };

            _itemOverlayContainer?.AddChild(colorRect);

            // PHASE 3: Store node reference for direct hiding during drag
            _itemSpriteNodes[itemId] = colorRect;
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
    /// Renders green/red highlight sprites showing where multi-cell item would be placed (Phase 4: L-shape support).
    /// </summary>
    /// <param name="origin">Top-left position where item would be placed</param>
    /// <param name="itemId">Item being dragged (to get shape)</param>
    /// <param name="rotation">Current drag rotation</param>
    /// <param name="isValid">True for green (valid placement), false for red (invalid)</param>
    private void RenderDragHighlight(GridPosition origin, ItemId itemId, Darklands.Core.Domain.Common.Rotation rotation, bool isValid)
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

        // PHASE 4: Get item's BASE shape (unrotated) from cache or query
        ItemShape baseShape;
        if (_itemShapes.TryGetValue(itemId, out var cachedShape))
        {
            // Shape in cache: This is the BASE shape (as stored in TileSet)
            baseShape = cachedShape;
        }
        else
        {
            // Cross-container drag: Item not in cache, fetch from repository
            var itemQuery = new GetItemByIdQuery(itemId);
            var itemResult = _mediator.Send(itemQuery).Result; // Blocking OK for highlight rendering

            if (itemResult.IsSuccess)
            {
                // PHASE 4: ItemDto now exposes Shape - use it for accurate L/T-shape highlighting!
                baseShape = itemResult.Value.Shape;
            }
            else
            {
                // Ultimate fallback if query fails
                baseShape = ItemShape.CreateRectangle(1, 1).Value;
                _logger.LogWarning("Failed to fetch item {ItemId} for highlight rendering, using 1×1 fallback", itemId);
            }
        }

        // Apply rotation parameter to BASE shape (rotates correctly for both same-container and cross-container drags)
        var rotatedShape = baseShape;
        for (int i = 0; i < ((int)rotation / 90); i++)
        {
            var rotResult = rotatedShape.RotateClockwise();
            if (rotResult.IsSuccess)
                rotatedShape = rotResult.Value;
        }

        // EQUIPMENT SLOT FIX: Override shape to 1×1 for equipment slots (Diablo 2 pattern)
        // WHY: Equipment slots display items as 1×1 regardless of actual shape
        // Visual feedback: Single cell highlight, not the item's multi-cell L-shape
        bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly && _gridWidth == 1 && _gridHeight == 1;
        if (isEquipmentSlot)
        {
            // Force 1×1 highlight for equipment slots (ignore item's actual shape)
            rotatedShape = ItemShape.CreateRectangle(1, 1).Value;
            _logger.LogDebug("Equipment slot: Overriding highlight to 1×1 (item shape ignored)");
        }

        // Render highlight sprite for ONLY occupied cells (not bounding box!)
        int separationX = 2;
        int separationY = 2;

        foreach (var offset in rotatedShape.OccupiedCells)
        {
            float pixelX = (origin.X + offset.X) * (CellSize + separationX);
            float pixelY = (origin.Y + offset.Y) * (CellSize + separationY);

            var highlight = new TextureRect
            {
                Name = $"Highlight_{origin.X + offset.X}_{origin.Y + offset.Y}",
                Texture = atlasTexture,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                CustomMinimumSize = new Vector2(CellSize, CellSize),
                Position = new Vector2(pixelX, pixelY),
                MouseFilter = MouseFilterEnum.Ignore,
                // PHASE 3 FIX: Extreme transparency (0.25 = 25% opacity) - pragmatic solution
                // WHY: Can't fix z-order reliably, so make highlights barely visible
                // Trade-off: Faint glow, but items always clearly visible
                Modulate = new Color(1, 1, 1, 0.25f)
            };

            _highlightOverlayContainer.AddChild(highlight);
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
    private void CreateDragPreview(ItemId itemId)
    {
        // Query item data for sprite rendering (synchronously for immediate preview)
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = _mediator.Send(itemQuery).Result;

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

        // PHASE 3 FIX: For drag preview, use BASE-sized container (simpler than two-layer)
        // WHY: Drag preview doesn't need to occupy cells - just needs to rotate naturally
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

        // Create sprite preview (single-layer: texture fills container, rotates around center)
        _dragPreviewSprite = new TextureRect
        {
            Texture = atlasTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Size = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Position = Vector2.Zero,
            Rotation = RotationHelper.ToRadians(_sharedDragRotation),
            PivotOffset = new Vector2(baseSpriteWidth / 2f, baseSpriteHeight / 2f),
            Modulate = new Color(1, 1, 1, 0.8f)
        };

        // Root for preview (engine positions this at the mouse)
        var previewRoot = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore
        };

        // Child offset container so the cursor sits at the sprite's CENTER
        var offsetContainer = new Control
        {
            Position = new Vector2(-baseSpriteWidth / 2f, -baseSpriteHeight / 2f),
            CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Size = new Vector2(baseSpriteWidth, baseSpriteHeight)
        };
        offsetContainer.AddChild(_dragPreviewSprite);
        previewRoot.AddChild(offsetContainer);
        _dragPreviewNode = previewRoot;
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
