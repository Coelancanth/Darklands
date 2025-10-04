using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Item.Application.Queries;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Components.Inventory;

/// <summary>
/// Equipment slot component for single-item swap UX (weapon, armor, ring slots).
/// Simplified from SpatialInventoryContainerNode - NO rotation, NO multi-cell, focused on SWAP operations.
/// </summary>
/// <remarks>
/// ARCHITECTURE (TD_003 - Equipment Slot Separation):
/// - 1×1 grid (single slot, no GridContainer complexity)
/// - Swap-focused (can swap occupied slots or move to empty)
/// - Type filtering (weapon slots reject potions - Core validates)
/// - Centered sprite scaling (fit item sprite in cell, preserve aspect ratio)
/// - No rotation support (equipment slots always display items unrotated)
///
/// CORE INTEGRATION (Delegates ALL business logic):
/// - CanPlaceItemAtQuery: Validates type compatibility + bounds
/// - SwapItemsCommand: Handles atomic swap with rollback
/// - MoveItemBetweenContainersCommand: Places item in empty slot
/// - GetItemByIdQuery: Loads item metadata (name, type, atlas coords)
/// - GetInventoryQuery: Loads slot contents
///
/// DESIGN TRADE-OFFS:
/// - Simpler than SpatialInventoryContainerNode (~400 lines vs 1293 lines)
/// - No shared code yet (Phase 2 will extract InventoryRenderHelper for DRY)
/// - Reusable for character sheets (6 equipment slots × 400 lines = 2400 lines vs 7758 lines)
/// </remarks>
public partial class EquipmentSlotNode : Control
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT SIGNALS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Emitted when slot contents change (item swapped/moved).
    /// WHY: Parent controller subscribes to refresh ALL containers.
    /// </summary>
    [Signal]
    public delegate void InventoryChangedEventHandler();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Actor ID for this equipment slot (assign via code from parent controller).
    /// </summary>
    public ActorId? OwnerActorId { get; set; }

    /// <summary>
    /// Slot title (displayed above slot: "Weapon", "Armor", "Ring", etc.).
    /// </summary>
    [Export] public string SlotTitle { get; set; } = "Weapon";

    /// <summary>
    /// Cell size in pixels (default: 96×96 for equipment slots - larger than inventory cells).
    /// </summary>
    [Export] public int CellSize { get; set; } = 96;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES (injected via properties before AddChild)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public IMediator? Mediator { get; set; }
    public TileSet? ItemTileSet { get; set; }

    private IMediator _mediator = null!;
    private ILogger<EquipmentSlotNode> _logger = null!;
    private TileSet? _itemTileSet;

    // Slot state
    private ItemId? _currentItemId = null; // null = empty slot
    private string? _currentItemName = null;
    private string? _currentItemType = null;

    // UI nodes
    private Label? _titleLabel;
    private Panel? _slotPanel;
    private Control? _itemOverlayContainer;
    private Control? _highlightOverlayContainer;

    // TileSet atlas coordinates for drag highlight sprites
    private static readonly Vector2I HIGHLIGHT_GREEN_COORDS = new(1, 6);
    private static readonly Vector2I HIGHLIGHT_RED_COORDS = new(1, 7);

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
        _itemTileSet = ItemTileSet;

        if (OwnerActorId == null)
        {
            _logger.LogError("OwnerActorId not assigned");
            return;
        }

        BuildUI();
        _ = LoadSlotAsync();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DRAG-DROP SYSTEM (Simplified for 1×1 swap-focused UX)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Notification(int what)
    {
        base._Notification(what);

        // Clear highlights when mouse exits slot during drag
        if (what == NotificationMouseExit)
        {
            ClearHighlights();
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // Clear highlights when drag ends (mouse released)
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed)
            {
                ClearHighlights();

                // Refresh to restore sprite if drag was cancelled
                _ = LoadSlotAsync();
            }
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (_currentItemId == null)
        {
            _logger.LogDebug("Cannot drag - slot is empty");
            return default; // Empty slot - nothing to drag
        }

        _logger.LogInformation("Starting drag: Item {ItemId} from {SlotTitle}",
            _currentItemId, SlotTitle);

        // Hide item sprite during drag
        ClearItemSprite();

        // Create drag preview
        var preview = CreateDragPreview(_currentItemId.Value);
        if (preview != null)
        {
            SetDragPreview(preview);
        }

        // Return drag data (equipment slots always at 0,0 - single cell)
        var dragData = new Godot.Collections.Dictionary
        {
            ["itemIdGuid"] = _currentItemId.Value.Value.ToString(),
            ["sourceActorIdGuid"] = OwnerActorId?.Value.ToString() ?? string.Empty,
            ["sourceX"] = 0, // Equipment slots are always 1×1 at origin
            ["sourceY"] = 0
        };

        return dragData;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
        {
            ClearHighlights();
            return false;
        }

        var dragData = data.AsGodotDictionary();
        if (!dragData.ContainsKey("itemIdGuid"))
        {
            ClearHighlights();
            return false;
        }

        var itemIdGuidStr = dragData["itemIdGuid"].AsString();
        if (!Guid.TryParse(itemIdGuidStr, out var itemIdGuid))
        {
            ClearHighlights();
            return false;
        }

        var itemId = new ItemId(itemIdGuid);

        if (OwnerActorId == null)
        {
            ClearHighlights();
            return false;
        }

        // Delegate validation to Core (checks type compatibility, bounds)
        var canPlaceQuery = new CanPlaceItemAtQuery(
            OwnerActorId.Value,
            itemId,
            new GridPosition(0, 0), // Equipment slots always at origin
            default(Rotation)); // Equipment slots don't rotate (Rotation.None)

        var validationResult = _mediator.Send(canPlaceQuery).Result; // Blocking OK for UI validation
        bool isValid = validationResult.IsSuccess && validationResult.Value;

        // Render highlight (green = valid, red = invalid)
        RenderHighlight(isValid);

        _logger.LogDebug("Can drop in {SlotTitle}: {IsValid}", SlotTitle, isValid);
        return isValid;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        _logger.LogInformation("_DropData called on {SlotTitle}", SlotTitle);

        var dragData = data.AsGodotDictionary();
        ClearHighlights();

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

        var sourceX = dragData["sourceX"].AsInt32();
        var sourceY = dragData["sourceY"].AsInt32();
        var sourcePos = new GridPosition(sourceX, sourceY);

        // Query inventory to check if slot is occupied (instead of relying on cached _currentItemId)
        // WHY: LoadSlotAsync() might not have completed yet, causing _currentItemId to be null
        if (OwnerActorId == null)
        {
            _logger.LogError("OwnerActorId is null - cannot determine swap vs move");
            return;
        }

        var inventoryQuery = new GetInventoryQuery(OwnerActorId.Value);
        var inventoryResult = _mediator.Send(inventoryQuery).Result; // Blocking OK for drop handler

        if (inventoryResult.IsFailure)
        {
            _logger.LogError("Failed to query inventory: {Error}", inventoryResult.Error);
            return;
        }

        var inventory = inventoryResult.Value;
        var slotPos = new GridPosition(0, 0); // Equipment slots always at origin
        var targetItemId = inventory.ItemPlacements
            .Where(kvp => kvp.Value.Equals(slotPos))
            .Select(kvp => (ItemId?)kvp.Key)
            .FirstOrDefault();

        // Check if this is a swap (slot occupied) or move (slot empty)
        if (targetItemId == null)
        {
            _logger.LogInformation("MOVE: Item {ItemId} to empty {SlotTitle}", itemId, SlotTitle);
            MoveItemAsync(sourceActorId, itemId);
        }
        else
        {
            _logger.LogInformation("SWAP: {ItemA} ↔ {ItemB} in {SlotTitle}",
                itemId, targetItemId, SlotTitle);
            SwapItemsAsync(sourceActorId, itemId, sourcePos, targetItemId.Value);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void BuildUI()
    {
        var vbox = new VBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        AddChild(vbox);

        // Title
        _titleLabel = new Label
        {
            Text = SlotTitle,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(_titleLabel);

        // Slot panel + overlays
        var slotWrapper = new Control { MouseFilter = MouseFilterEnum.Pass };
        vbox.AddChild(slotWrapper);

        _slotPanel = new Panel
        {
            CustomMinimumSize = new Vector2(CellSize, CellSize),
            MouseFilter = MouseFilterEnum.Pass
        };

        // Equipment slot styling (darker with gold border)
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f), // Darker than regular inventory
            BorderColor = new Color(0.6f, 0.5f, 0.3f) // Gold tint for equipment
        };
        style.SetBorderWidthAll(2);
        _slotPanel.AddThemeStyleboxOverride("panel", style);

        slotWrapper.AddChild(_slotPanel);

        // Overlays (highlights below, items above)
        _highlightOverlayContainer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        slotWrapper.AddChild(_highlightOverlayContainer);

        _itemOverlayContainer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        slotWrapper.AddChild(_itemOverlayContainer);
    }

    private async Task LoadSlotAsync()
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

        // Find item in slot (equipment slots have 1×1 grid, so check position 0,0)
        var slotPos = new GridPosition(0, 0);
        _currentItemId = inventory.ItemPlacements
            .Where(kvp => kvp.Value.Equals(slotPos))
            .Select(kvp => (ItemId?)kvp.Key)
            .FirstOrDefault();

        if (_currentItemId != null)
        {
            // Load item metadata
            var itemQuery = new GetItemByIdQuery(_currentItemId.Value);
            var itemResult = await _mediator.Send(itemQuery);

            if (itemResult.IsSuccess)
            {
                _currentItemName = itemResult.Value.Name;
                _currentItemType = itemResult.Value.Type;
                _logger.LogDebug("Slot {SlotTitle} contains: {ItemName} ({ItemType})",
                    SlotTitle, _currentItemName, _currentItemType);
            }
        }

        UpdateDisplayInternal();
    }

    private void UpdateDisplayInternal()
    {
        ClearItemSprite();

        if (_currentItemId == null)
        {
            // Empty slot
            if (_titleLabel != null)
            {
                _titleLabel.Text = $"{SlotTitle} (Empty)";
            }
            if (_slotPanel != null)
            {
                _slotPanel.TooltipText = $"{SlotTitle} - Empty";
            }
            return;
        }

        // Occupied slot
        if (_titleLabel != null)
        {
            _titleLabel.Text = SlotTitle;
        }

        if (_slotPanel != null)
        {
            _slotPanel.TooltipText = $"{_currentItemName ?? "Item"} ({_currentItemType ?? "unknown"})";
        }

        RenderItemSprite(_currentItemId.Value);
    }

    private async void RenderItemSprite(ItemId itemId)
    {
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = await _mediator.Send(itemQuery);

        if (itemResult.IsFailure || _itemTileSet == null)
        {
            _logger.LogWarning("Failed to render item sprite: {Error}",
                itemResult.IsFailure ? itemResult.Error : "No TileSet");
            return;
        }

        var item = itemResult.Value;
        var atlasSource = _itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
            return;

        // Extract sprite from atlas
        var tileCoords = new Vector2I(item.AtlasX, item.AtlasY);
        var region = atlasSource.GetTileTextureRegion(tileCoords);

        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        // Equipment slots ALWAYS scale to fit cell (centered, preserve aspect ratio)
        // WHY: Same behavior as SpatialInventoryContainerNode lines 870-893
        float actualTextureWidth = region.Size.X;
        float actualTextureHeight = region.Size.Y;

        // Scale to fit in CellSize (preserve aspect ratio)
        float scale = Math.Min(CellSize / actualTextureWidth, CellSize / actualTextureHeight);
        float textureWidth = actualTextureWidth * scale;
        float textureHeight = actualTextureHeight * scale;

        // Center in cell
        float texturePosX = (CellSize - textureWidth) / 2f;
        float texturePosY = (CellSize - textureHeight) / 2f;

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
            MouseFilter = MouseFilterEnum.Ignore,
            Rotation = 0, // Equipment slots never rotate (line 927 from original)
            ZIndex = 100
        };

        _itemOverlayContainer?.AddChild(textureRect);

        _logger.LogDebug("Rendered item {ItemId} in {SlotTitle}: {TW}×{TH} scaled to {SW}×{SH}",
            itemId, SlotTitle, actualTextureWidth, actualTextureHeight, textureWidth, textureHeight);
    }

    private void ClearItemSprite()
    {
        if (_itemOverlayContainer == null)
            return;

        foreach (Node child in _itemOverlayContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void RenderHighlight(bool isValid)
    {
        if (_highlightOverlayContainer == null || _itemTileSet == null)
            return;

        ClearHighlights();

        var atlasSource = _itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
            return;

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
            CustomMinimumSize = new Vector2(CellSize, CellSize),
            Position = Vector2.Zero,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(1, 1, 1, 0.4f) // Slightly more visible than inventory highlights
        };

        _highlightOverlayContainer.AddChild(highlight);
    }

    private void ClearHighlights()
    {
        if (_highlightOverlayContainer == null)
            return;

        foreach (Node child in _highlightOverlayContainer.GetChildren())
        {
            child.Free(); // Immediate removal
        }
    }

    private Control? CreateDragPreview(ItemId itemId)
    {
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = _mediator.Send(itemQuery).Result;

        if (itemResult.IsFailure || _itemTileSet == null)
        {
            return new Label { Text = _currentItemName ?? "Item" };
        }

        var item = itemResult.Value;
        var atlasSource = _itemTileSet.GetSource(0) as TileSetAtlasSource;
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

        // Drag preview size (slightly smaller for visual clarity)
        float previewSize = CellSize * 0.8f;

        var sprite = new TextureRect
        {
            Texture = atlasTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(previewSize, previewSize),
            Size = new Vector2(previewSize, previewSize),
            Modulate = new Color(1, 1, 1, 0.8f)
        };

        // Center at cursor
        var previewRoot = new Control { MouseFilter = MouseFilterEnum.Ignore };
        var offsetContainer = new Control
        {
            Position = new Vector2(-previewSize / 2f, -previewSize / 2f),
            CustomMinimumSize = new Vector2(previewSize, previewSize)
        };
        offsetContainer.AddChild(sprite);
        previewRoot.AddChild(offsetContainer);

        return previewRoot;
    }

    private async void SwapItemsAsync(
        ActorId sourceActorId,
        ItemId sourceItemId,
        GridPosition sourcePos,
        ItemId targetItemId)
    {
        _logger.LogInformation("SWAP: Delegating to Core SwapItemsCommand");

        var command = new SwapItemsCommand(
            sourceActorId,
            sourceItemId,
            sourcePos,
            OwnerActorId!.Value,
            targetItemId,
            new GridPosition(0, 0), // Equipment slots always at origin
            default(Rotation)); // Equipment slots don't rotate (Rotation.None)

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogError("SWAP FAILED: {Error}", result.Error);
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        _logger.LogInformation("SWAP COMPLETED");
        EmitSignal(SignalName.InventoryChanged);
    }

    private async void MoveItemAsync(ActorId sourceActorId, ItemId itemId)
    {
        if (OwnerActorId == null)
            return;

        var command = new MoveItemBetweenContainersCommand(
            sourceActorId,
            OwnerActorId.Value,
            itemId,
            new GridPosition(0, 0), // Equipment slots always at origin
            default(Rotation)); // Equipment slots don't rotate (Rotation.None)

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to move item: {Error}", result.Error);
            return;
        }

        _logger.LogInformation("Item moved to {SlotTitle}", SlotTitle);
        EmitSignal(SignalName.InventoryChanged);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PUBLIC API (for external refresh triggers)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Reloads slot data and refreshes display.
    /// WHY: Called by parent controller after swaps/moves to sync all displays.
    /// </summary>
    public void RefreshDisplay()
    {
        _ = LoadSlotAsync();
    }
}
