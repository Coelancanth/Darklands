using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Application.Commands;
using Darklands.Core.Features.Equipment.Domain;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Item.Application;
using Darklands.Core.Features.Item.Application.Queries;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Presentation.Features.Inventory;

/// <summary>
/// Equipment slot component for single-item equipment UX (weapon, armor slots).
/// VS_032 Phase 4: Refactored to use EquipmentComponent (not Inventory-based).
/// </summary>
/// <remarks>
/// ARCHITECTURE (VS_032 Phase 4 - Equipment System):
/// - Parent-driven data pattern: EquipmentPanelNode queries, slots render
/// - Single equipment slot (MainHand, OffHand, Head, Torso, Legs)
/// - Type filtering (weapon slots reject armor, etc.)
/// - Centered sprite scaling (fit item sprite in cell, preserve aspect ratio)
/// - No rotation support (equipment slots always display items unrotated)
///
/// CORE INTEGRATION (Delegates ALL business logic):
/// - EquipItemCommand: Moves item from inventory → equipment slot
/// - UnequipItemCommand: Moves item from equipment slot → inventory
/// - SwapEquipmentCommand: Atomic swap of equipped item with inventory item
/// - GetItemByIdQuery: Loads item metadata (name, type, atlas coords)
/// - GetEquippedItemsQuery: Loads all equipment (queried by parent panel)
///
/// DATA FLOW:
/// - Parent EquipmentPanelNode queries equipment state (all 5 slots at once)
/// - Parent calls UpdateDisplay(ItemDto?) to push data to this slot
/// - Slot renders (no queries) - "dumb renderer" pattern
/// - Slot sends commands on drag-drop (EquipItemCommand, etc.)
/// - Slot emits InventoryChanged signal → parent re-queries
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
    /// Inventory ID for the player's inventory (where unequipped items go).
    /// TD_019 Phase 4: Required for UnequipItemCommand (target inventory).
    /// </summary>
    public Darklands.Core.Features.Inventory.Domain.InventoryId? PlayerInventoryId { get; set; }

    /// <summary>
    /// Equipment slot type (MainHand, OffHand, Head, Torso, Legs).
    /// VS_032 Phase 4: Added for new equipment system.
    /// </summary>
    public EquipmentSlot Slot { get; set; }

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
    private bool _isDragging = false; // Track drag state for sprite restoration

    // UI nodes
    private Label? _titleLabel;
    private Panel? _slotPanel;
    private Control? _itemOverlayContainer;
    private Control? _highlightOverlayContainer;

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

        if (PlayerInventoryId == null)
        {
            _logger.LogError("PlayerInventoryId not assigned");
            return;
        }

        BuildUI();
        // VS_032 Phase 4: Removed LoadSlotAsync() - parent panel pushes data via UpdateDisplay()
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

        // Handle drag cancellation (mouse released without successful drop)
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed)
            {
                ClearHighlights();

                // VS_032 Phase 4 FIX: If drag was cancelled (invalid drop), restore sprite
                // WHY: _DropData() only called on VALID drops, invalid drops leave sprite hidden
                if (_isDragging)
                {
                    _logger.LogDebug("Drag ended without successful drop - restoring sprite");
                    _isDragging = false;
                    EmitSignal(SignalName.InventoryChanged); // Triggers parent panel refresh
                }
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

        // Hide item sprite during drag & set dragging flag
        ClearItemSprite();
        _isDragging = true;

        // BR_009 FIX: Equipment slots always use 0° rotation (canonical orientation)
        // WHY: Equipment doesn't rotate (industry standard), always shows items at default orientation
        InventoryContainerNode._sharedDragRotation = Core.Domain.Common.Rotation.Degrees0;

        // Create drag preview
        var preview = CreateDragPreview(_currentItemId.Value);
        if (preview != null)
        {
            SetDragPreview(preview);
        }

        // Return drag data with source slot info (VS_032 Phase 4: enables equipment-to-equipment transfers)
        var dragData = new Godot.Collections.Dictionary
        {
            ["itemIdGuid"] = _currentItemId.Value.Value.ToString(),
            ["sourceActorIdGuid"] = OwnerActorId?.Value.ToString() ?? string.Empty,
            ["sourceX"] = 0, // Equipment slots are always 1×1 at origin
            ["sourceY"] = 0,
            ["sourceSlot"] = (int)Slot,  // NEW: Identifies drag source as equipment slot
            ["sourceContainerType"] = "equipment"  // NEW: Helps target distinguish equipment vs inventory sources
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

        // VS_032 Phase 4: Equipment validation (simplified from inventory-based pattern)
        // Use cached _currentItemId to check if slot is occupied (no query needed!)
        // Then validate item type matches slot (weapon/armor/etc.)

        bool slotIsOccupied = _currentItemId != null;

        // Query item type for validation
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = _mediator.Send(itemQuery).Result;

        if (itemResult.IsFailure)
        {
            ClearHighlights();
            return false;
        }

        var item = itemResult.Value;

        // Validate item type matches slot type
        bool isValid = ValidateItemTypeForSlot(item.Type, Slot);

        if (!isValid)
        {
            _logger.LogDebug("Item type {ItemType} not valid for slot {Slot}", item.Type, Slot);
        }

        // Render highlight (green = valid, red = invalid)
        RenderHighlight(isValid);

        return isValid;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        _logger.LogInformation("_DropData called on {SlotTitle}", SlotTitle);

        var dragData = data.AsGodotDictionary();
        ClearHighlights();

        // Mark drag as completed (successful drop)
        _isDragging = false;

        // TD_019 Phase 4 FIX: Check source type FIRST before reading sourceActorIdGuid
        // WHY: Equipment drags have sourceActorIdGuid, inventory drags have sourceInventoryIdGuid
        bool isEquipmentSource = dragData.ContainsKey("sourceSlot");

        var itemIdGuidStr = dragData["itemIdGuid"].AsString();
        if (!Guid.TryParse(itemIdGuidStr, out var itemIdGuid))
        {
            _logger.LogError("Failed to parse itemIdGuid");
            return;
        }

        var itemId = new ItemId(itemIdGuid);

        if (OwnerActorId == null || PlayerInventoryId == null)
        {
            _logger.LogError("OwnerActorId or PlayerInventoryId is null - cannot equip item");
            return;
        }

        if (isEquipmentSource)
        {
            // Source: Equipment Slot → Target: Equipment Slot (Option A)
            // TD_019 Phase 4: Equipment drags use sourceActorIdGuid (not sourceInventoryIdGuid)
            var sourceSlot = (EquipmentSlot)dragData["sourceSlot"].AsInt32();
            HandleEquipmentToEquipmentTransfer(itemId, sourceSlot);
        }
        else
        {
            // Source: Inventory → Target: Equipment Slot (original behavior)
            // TD_019 Phase 4: Inventory drags have sourceInventoryIdGuid - we need to pass the actual source inventory
            var sourceInventoryIdGuidStr = dragData["sourceInventoryIdGuid"].AsString();
            if (!Guid.TryParse(sourceInventoryIdGuidStr, out var sourceInventoryIdGuid))
            {
                _logger.LogError("Failed to parse sourceInventoryIdGuid");
                return;
            }
            var sourceInventoryId = new Darklands.Core.Features.Inventory.Domain.InventoryId(sourceInventoryIdGuid);

            HandleInventoryToEquipmentTransfer(itemId, sourceInventoryId);
        }
    }

    /// <summary>
    /// Handles dragging from inventory to equipment slot.
    /// VS_032 Phase 4: Original behavior - equip from inventory.
    /// TD_019 Phase 4: Now accepts sourceInventoryId to support cross-inventory equips.
    /// </summary>
    private void HandleInventoryToEquipmentTransfer(ItemId itemId, Darklands.Core.Features.Inventory.Domain.InventoryId sourceInventoryId)
    {
        // Check if this is a swap (slot occupied) or equip (slot empty)
        if (_currentItemId == null)
        {
            // Empty slot → Equip item from inventory
            _logger.LogInformation("EQUIP from inventory: Item {ItemId} to empty {SlotTitle} ({Slot})",
                itemId, SlotTitle, Slot);
            EquipItemAsync(itemId, sourceInventoryId);
        }
        else if (_currentItemId.Value.Equals(itemId))
        {
            // Self-swap detected: dragging item back to its own slot
            _logger.LogDebug("Self-equip detected for {ItemId} - ignoring", itemId);
            EmitSignal(SignalName.InventoryChanged); // Trigger refresh to restore sprite
        }
        else
        {
            // Occupied slot → Swap: unequip current, equip new
            _logger.LogInformation("SWAP from inventory: {NewItem} ↔ {OldItem} in {SlotTitle} ({Slot})",
                itemId, _currentItemId, SlotTitle, Slot);
            SwapEquipmentAsync(itemId, sourceInventoryId);
        }
    }

    /// <summary>
    /// Handles dragging from one equipment slot to another.
    /// VS_032 Phase 4: Option A - equipment-to-equipment transfers.
    /// </summary>
    private void HandleEquipmentToEquipmentTransfer(ItemId itemId, EquipmentSlot sourceSlot)
    {
        // Don't allow dragging to same slot
        if (sourceSlot == Slot)
        {
            _logger.LogDebug("Dragging to same slot - ignoring");
            EmitSignal(SignalName.InventoryChanged); // Refresh to restore sprite
            return;
        }

        if (_currentItemId == null)
        {
            // Empty target slot → Move item between equipment slots
            _logger.LogInformation("MOVE equipment: Item {ItemId} from {SourceSlot} to {TargetSlot}",
                itemId, sourceSlot, Slot);
            MoveEquipmentAsync(itemId, sourceSlot, Slot);
        }
        else
        {
            // Occupied target slot → Swap items between equipment slots
            _logger.LogInformation("SWAP equipment: {SourceSlot} ↔ {TargetSlot}",
                sourceSlot, Slot);
            SwapEquipmentSlotsAsync(sourceSlot, Slot);
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

    /// <summary>
    /// Updates slot display with equipment data from parent panel.
    /// VS_032 Phase 4: Parent-driven pattern replaces self-loading LoadSlotAsync().
    /// </summary>
    /// <param name="item">Item DTO from parent (null if slot is empty)</param>
    /// <remarks>
    /// PATTERN: Parent panel queries GetEquippedItemsQuery + GetItemByIdQuery,
    /// then pushes ItemDto to each child slot. Slot just renders (no queries).
    /// </remarks>
    public void UpdateDisplay(ItemDto? item)
    {
        if (item != null)
        {
            _currentItemId = item.Id; // ItemDto.Id is already ItemId type
            _currentItemName = item.Name;
            _currentItemType = item.Type;
            _logger.LogDebug("Slot {SlotTitle} ({Slot}) updated with item {ItemName} ({ItemType})",
                SlotTitle, Slot, _currentItemName, _currentItemType);
        }
        else
        {
            _currentItemId = null;
            _currentItemName = null;
            _currentItemType = null;
            _logger.LogDebug("Slot {SlotTitle} ({Slot}) updated as empty", SlotTitle, Slot);
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
        // TD_003 Phase 2: Use InventoryRenderHelper (DRY)
        var sprite = await InventoryRenderHelper.CreateItemSpriteAsync(
            itemId,
            _mediator,
            _itemTileSet,
            CellSize,
            shouldCenter: true,  // Equipment slots center sprites
            rotation: 0f,        // Equipment slots never rotate
            _logger);

        if (sprite != null && _itemOverlayContainer != null)
        {
            _itemOverlayContainer.AddChild(sprite);
        }
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
        if (_highlightOverlayContainer == null)
            return;

        ClearHighlights();

        // TD_003 Phase 2: Use InventoryRenderHelper (DRY)
        var highlight = InventoryRenderHelper.CreateHighlight(
            isValid,
            _itemTileSet,
            CellSize,
            opacity: 0.4f);

        if (highlight != null)
        {
            _highlightOverlayContainer.AddChild(highlight);
        }
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

    /// <summary>
    /// BR_009 FIX: Create rotatable drag preview (matches InventoryContainerNode pattern).
    /// WHY: Equipment drags need to support scroll wheel rotation during drag.
    /// </summary>
    private Control? CreateDragPreview(ItemId itemId)
    {
        // Query item data for sprite rendering
        var itemQuery = new GetItemByIdQuery(itemId);
        var itemResult = _mediator.Send(itemQuery).Result;

        if (itemResult.IsFailure || _itemTileSet == null)
        {
            // Fallback: Simple label
            return new Label { Text = _currentItemName ?? "Item" };
        }

        var item = itemResult.Value;
        var atlasSource = _itemTileSet.GetSource(0) as TileSetAtlasSource;
        if (atlasSource == null)
        {
            return new Label { Text = item.Name };
        }

        // Equipment slots are 1×1 (no multi-cell items)
        float baseSpriteWidth = CellSize;
        float baseSpriteHeight = CellSize;

        // Extract sprite from atlas
        var tileCoords = new Vector2I(item.AtlasX, item.AtlasY);
        var region = atlasSource.GetTileTextureRegion(tileCoords);
        var atlasTexture = new AtlasTexture
        {
            Atlas = atlasSource.Texture,
            Region = region
        };

        // Create SHARED rotatable sprite preview (matches InventoryContainerNode pattern)
        InventoryContainerNode._sharedDragPreviewSprite = new TextureRect
        {
            Texture = atlasTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Size = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Position = Vector2.Zero,
            Rotation = RotationHelper.ToRadians(InventoryContainerNode._sharedDragRotation), // Starts at 0° (set in _GetDragData)
            PivotOffset = new Vector2(baseSpriteWidth / 2f, baseSpriteHeight / 2f),
            Modulate = new Color(1, 1, 1, 0.8f)
        };

        // Root container for preview (engine positions at mouse)
        var previewRoot = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        // Offset container centers cursor on sprite
        var offsetContainer = new Control
        {
            Position = new Vector2(-baseSpriteWidth / 2f, -baseSpriteHeight / 2f),
            CustomMinimumSize = new Vector2(baseSpriteWidth, baseSpriteHeight),
            Size = new Vector2(baseSpriteWidth, baseSpriteHeight)
        };
        offsetContainer.AddChild(InventoryContainerNode._sharedDragPreviewSprite);
        previewRoot.AddChild(offsetContainer);

        return previewRoot;
    }

    /// <summary>
    /// Equips an item from inventory to this equipment slot.
    /// VS_032 Phase 4: Uses EquipItemCommand (not MoveItemBetweenContainersCommand).
    /// TD_019 Phase 4: Accepts sourceInventoryId to support cross-inventory equips (e.g., enemy loot → player equipment).
    /// </summary>
    private async void EquipItemAsync(ItemId itemId, Darklands.Core.Features.Inventory.Domain.InventoryId sourceInventoryId)
    {
        if (OwnerActorId == null || PlayerInventoryId == null)
            return;

        _logger.LogInformation("Equipping item {ItemId} from inventory {SourceInventoryId} to {Slot}", itemId, sourceInventoryId, Slot);

        // TD_019 Phase 4: Use actual sourceInventoryId from drag data (not hardcoded PlayerInventoryId)
        var command = new EquipItemCommand(
            OwnerActorId.Value,
            sourceInventoryId,
            itemId,
            Slot,
            false); // Positional parameter for IsTwoHanded

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to equip item: {Error}", result.Error);
            EmitSignal(SignalName.InventoryChanged); // Trigger refresh even on failure
            return;
        }

        _logger.LogInformation("Item equipped to {SlotTitle} ({Slot})", SlotTitle, Slot);
        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Swaps equipped item with inventory item.
    /// VS_032 Phase 4: Uses SwapEquipmentCommand (not SwapItemsCommand).
    /// TD_019 Phase 4: Accepts sourceInventoryId to support cross-inventory swaps.
    /// </summary>
    private async void SwapEquipmentAsync(ItemId newItemId, Darklands.Core.Features.Inventory.Domain.InventoryId sourceInventoryId)
    {
        if (OwnerActorId == null || PlayerInventoryId == null)
            return;

        _logger.LogInformation("Swapping equipment in {Slot}: {NewItem} from inventory {SourceInventoryId} ↔ {OldItem}",
            Slot, newItemId, sourceInventoryId, _currentItemId);

        // TD_019 Phase 4: Use actual sourceInventoryId from drag data (not hardcoded PlayerInventoryId)
        var command = new SwapEquipmentCommand(
            OwnerActorId.Value,
            sourceInventoryId,
            newItemId,
            Slot,
            false); // Positional parameter for IsTwoHanded

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to swap equipment: {Error}", result.Error);
            EmitSignal(SignalName.InventoryChanged); // Trigger refresh even on failure
            return;
        }

        _logger.LogInformation("Equipment swapped in {SlotTitle} ({Slot})", SlotTitle, Slot);
        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Moves item from one equipment slot to another (empty target).
    /// VS_032 Phase 4 Option A: Equipment-to-equipment transfer.
    /// </summary>
    /// <remarks>
    /// Implementation: Unequip from source → Equip to target (2-step atomic operation).
    /// WHY: Core doesn't have MoveEquipmentCommand - we compose from existing commands.
    /// </remarks>
    private async void MoveEquipmentAsync(ItemId itemId, EquipmentSlot sourceSlot, EquipmentSlot targetSlot)
    {
        if (OwnerActorId == null || PlayerInventoryId == null)
            return;

        _logger.LogInformation("Moving item {ItemId} from {SourceSlot} to {TargetSlot}",
            itemId, sourceSlot, targetSlot);

        // Step 1: Unequip from source slot
        // TD_019 Phase 4: UnequipItemCommand signature: (ActorId, InventoryId, EquipmentSlot)
        var unequipCommand = new UnequipItemCommand(OwnerActorId.Value, PlayerInventoryId.Value, sourceSlot);
        var unequipResult = await _mediator.Send(unequipCommand);

        if (unequipResult.IsFailure)
        {
            _logger.LogError("Failed to unequip from {SourceSlot}: {Error}", sourceSlot, unequipResult.Error);
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        // Step 2: Equip to target slot
        // TD_019 Phase 4: EquipItemCommand now requires sourceInventoryId parameter
        var equipCommand = new EquipItemCommand(OwnerActorId.Value, PlayerInventoryId.Value, itemId, targetSlot, false);
        var equipResult = await _mediator.Send(equipCommand);

        if (equipResult.IsFailure)
        {
            _logger.LogError("Failed to equip to {TargetSlot}: {Error}", targetSlot, equipResult.Error);
            // NOTE: Item is now in inventory (from step 1) - not lost, just not where user expected
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        _logger.LogInformation("Item moved from {SourceSlot} to {TargetSlot}", sourceSlot, targetSlot);
        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Swaps items between two equipment slots (both occupied).
    /// VS_032 Phase 4 Option A: Equipment-to-equipment swap.
    /// </summary>
    /// <remarks>
    /// Implementation: Unequip slot A → Unequip slot B → Equip B's item to A → Equip A's item to B.
    /// WHY: Core's SwapEquipmentCommand expects inventory source, not equipment-to-equipment.
    /// RISK: Multi-step operation - if any step fails, items end up in inventory (safe, but unexpected UX).
    /// </remarks>
    private async void SwapEquipmentSlotsAsync(EquipmentSlot slotA, EquipmentSlot slotB)
    {
        if (OwnerActorId == null || PlayerInventoryId == null)
            return;

        _logger.LogInformation("Swapping items between {SlotA} and {SlotB}", slotA, slotB);

        // Get current items in both slots (before unequipping)
        var itemInSlotA = slotA == Slot ? _currentItemId : null;  // If slotA is THIS slot, use cached ID
        var itemInSlotB = slotB == Slot ? _currentItemId : null;  // If slotB is THIS slot, use cached ID

        // Need to query the OTHER slot's item
        // WHY: We only have cached state for THIS slot, not the source slot
        if (itemInSlotA == null || itemInSlotB == null)
        {
            _logger.LogError("Cannot swap - missing item IDs (slotA: {SlotA}, slotB: {SlotB})", slotA, slotB);
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        // Step 1: Unequip from slot A
        // TD_019 Phase 4: UnequipItemCommand signature: (ActorId, InventoryId, EquipmentSlot)
        var unequipA = new UnequipItemCommand(OwnerActorId.Value, PlayerInventoryId.Value, slotA);
        var resultA = await _mediator.Send(unequipA);

        if (resultA.IsFailure)
        {
            _logger.LogError("Failed to unequip from {SlotA}: {Error}", slotA, resultA.Error);
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        // Step 2: Unequip from slot B
        // TD_019 Phase 4: UnequipItemCommand signature: (ActorId, InventoryId, EquipmentSlot)
        var unequipB = new UnequipItemCommand(OwnerActorId.Value, PlayerInventoryId.Value, slotB);
        var resultB = await _mediator.Send(unequipB);

        if (resultB.IsFailure)
        {
            _logger.LogError("Failed to unequip from {SlotB}: {Error}", slotB, resultB.Error);
            // NOTE: Slot A's item is now in inventory - partial state, but recoverable
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        // Step 3: Equip B's item to slot A
        // TD_019 Phase 4: EquipItemCommand now requires sourceInventoryId parameter
        var equipToA = new EquipItemCommand(OwnerActorId.Value, PlayerInventoryId.Value, itemInSlotB.Value, slotA, false);
        var equipAResult = await _mediator.Send(equipToA);

        if (equipAResult.IsFailure)
        {
            _logger.LogError("Failed to equip to {SlotA}: {Error}", slotA, equipAResult.Error);
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        // Step 4: Equip A's item to slot B
        // TD_019 Phase 4: EquipItemCommand now requires sourceInventoryId parameter
        var equipToB = new EquipItemCommand(OwnerActorId.Value, PlayerInventoryId.Value, itemInSlotA.Value, slotB, false);
        var equipBResult = await _mediator.Send(equipToB);

        if (equipBResult.IsFailure)
        {
            _logger.LogError("Failed to equip to {SlotB}: {Error}", slotB, equipBResult.Error);
            // NOTE: Partial swap - B's item in A, A's item in inventory
            EmitSignal(SignalName.InventoryChanged);
            return;
        }

        _logger.LogInformation("Successfully swapped items between {SlotA} and {SlotB}", slotA, slotB);
        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Validates if item type matches equipment slot type.
    /// VS_032 Phase 4: Basic type checking (weapon/armor) - full type system in future.
    /// </summary>
    private bool ValidateItemTypeForSlot(string itemType, EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.MainHand => itemType == "weapon",
            EquipmentSlot.OffHand => itemType == "weapon", // TODO: Add "shield" type in future
            EquipmentSlot.Head => itemType == "armor",
            EquipmentSlot.Torso => itemType == "armor",
            EquipmentSlot.Legs => itemType == "armor",
            _ => false
        };
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PUBLIC API (for external refresh triggers)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // VS_032 Phase 4: RefreshDisplay() removed - parent panel owns refresh logic
    // Parent panel calls UpdateDisplay(ItemDto?) to push data to this slot
}
