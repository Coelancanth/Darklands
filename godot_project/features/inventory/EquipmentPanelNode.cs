using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Application.Queries;
using Darklands.Core.Features.Equipment.Domain;
using Darklands.Core.Features.Item.Application.Queries;
using Darklands.Core.Infrastructure.DependencyInjection;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Presentation.Features.Inventory;

/// <summary>
/// Equipment panel displaying all 5 equipment slots (MainHand, OffHand, Head, Torso, Legs).
/// Queries equipment state ONCE and pushes data to child EquipmentSlotNodes for rendering.
/// </summary>
/// <remarks>
/// ARCHITECTURE (VS_032 Phase 4):
/// - Parent-driven data pattern: Panel queries, slots render
/// - Efficiency: 1 query for all 5 slots (vs 5 individual queries)
/// - Unidirectional data flow: Commands up (slots → panel), data down (panel → slots)
///
/// INTEGRATION:
/// - Uses GetEquippedItemsQuery to load all equipped items at once
/// - Uses GetItemByIdQuery to fetch metadata (name, type, sprite coords)
/// - Child EquipmentSlotNodes emit InventoryChanged → panel re-queries → refreshes all slots
/// - Panel emits InventoryChanged to parent controller for cross-container refresh
///
/// COMPONENT HIERARCHY:
/// EquipmentPanelNode (this)
/// └── 5× EquipmentSlotNode (MainHand, OffHand, Head, Torso, Legs)
///     ├── Receives: ItemDto? from parent (push model)
///     ├── Sends: EquipItemCommand, UnequipItemCommand, SwapEquipmentCommand
///     └── Emits: InventoryChanged signal (caught by panel)
/// </remarks>
public partial class EquipmentPanelNode : Control
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT SIGNALS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Emitted when any equipment slot changes (item equipped/unequipped/swapped).
    /// WHY: Parent controller subscribes to refresh ALL containers (cross-container sync).
    /// </summary>
    [Signal]
    public delegate void InventoryChangedEventHandler();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Actor ID for this equipment panel (assign via code from parent controller).
    /// </summary>
    public ActorId? OwnerActorId { get; set; }

    /// <summary>
    /// Inventory ID for the player's inventory (where unequipped items go).
    /// TD_019 Phase 4: Required to pass to child EquipmentSlotNodes.
    /// </summary>
    public Darklands.Core.Features.Inventory.Domain.InventoryId? PlayerInventoryId { get; set; }

    /// <summary>
    /// Panel title (displayed above equipment slots).
    /// </summary>
    [Export] public string PanelTitle { get; set; } = "Equipment";

    /// <summary>
    /// Cell size in pixels for each equipment slot (default: 96×96 - larger than inventory cells).
    /// </summary>
    [Export] public int CellSize { get; set; } = 96;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES (injected via properties before AddChild)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public IMediator? Mediator { get; set; }
    public TileSet? ItemTileSet { get; set; }

    private IMediator _mediator = null!;
    private ILogger<EquipmentPanelNode> _logger = null!;
    private TileSet? _itemTileSet;

    // Equipment slot nodes (5 slots: MainHand, OffHand, Head, Torso, Legs)
    private readonly Dictionary<EquipmentSlot, EquipmentSlotNode> _slotNodes = new();

    // UI nodes
    private Label? _titleLabel;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve logger
        var loggerResult = ServiceLocator.GetService<ILogger<EquipmentPanelNode>>();

        if (loggerResult.IsFailure)
        {
            GD.PrintErr("[EquipmentPanelNode] Failed to resolve logger");
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
        RefreshDisplay(); // async void - no need for _ assignment
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PUBLIC API (for external refresh triggers)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Reloads equipment data and refreshes all slot displays.
    /// WHY: Called by parent controller after cross-container moves to sync all displays.
    /// </summary>
    /// <remarks>
    /// PATTERN: Parent-driven refresh
    /// 1. Query GetEquippedItemsQuery (all slots at once)
    /// 2. Query GetItemByIdQuery for each equipped item (metadata)
    /// 3. Push ItemDto to each child EquipmentSlotNode via UpdateDisplay()
    /// </remarks>
    public async void RefreshDisplay()
    {
        if (OwnerActorId == null)
            return;

        _logger.LogDebug("Refreshing equipment panel for actor {ActorId}", OwnerActorId);

        // Query all equipped items at once (efficient!)
        var query = new GetEquippedItemsQuery(OwnerActorId.Value);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to query equipped items: {Error}", result.Error);
            return;
        }

        var equippedItems = result.Value;

        _logger.LogDebug("Actor {ActorId} has {Count} items equipped", OwnerActorId, equippedItems.Count);

        // Update each slot with its data (or null if empty)
        foreach (var (slot, slotNode) in _slotNodes)
        {
            ItemDto? itemDto = null;

            if (equippedItems.TryGetValue(slot, out var itemId))
            {
                // Query item metadata
                var itemQuery = new GetItemByIdQuery(itemId);
                var itemResult = await _mediator.Send(itemQuery);

                if (itemResult.IsSuccess)
                {
                    itemDto = itemResult.Value;
                    _logger.LogDebug("Slot {Slot} has item {ItemName} ({ItemId})", slot, itemDto.Name, itemId);
                }
                else
                {
                    _logger.LogWarning("Failed to query item {ItemId} for slot {Slot}: {Error}",
                        itemId, slot, itemResult.Error);
                }
            }
            else
            {
                _logger.LogDebug("Slot {Slot} is empty", slot);
            }

            // Push data to child slot (synchronous update)
            slotNode.UpdateDisplay(itemDto);
        }

        _logger.LogDebug("Equipment panel refresh complete");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void BuildUI()
    {
        // Set panel size to contain all slots
        CustomMinimumSize = new Vector2(150, 600); // Width for slot + labels, height for 5 slots + spacing

        var vbox = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.Fill
        };
        vbox.AddThemeConstantOverride("separation", 8); // Add spacing between slots
        AddChild(vbox);

        // Title
        _titleLabel = new Label
        {
            Text = PanelTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(0, 30)
        };
        vbox.AddChild(_titleLabel);

        // Create 5 equipment slots (MainHand, OffHand, Head, Torso, Legs)
        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            var slotNode = new EquipmentSlotNode
            {
                Slot = slot,
                SlotTitle = GetSlotTitle(slot),
                OwnerActorId = OwnerActorId,
                PlayerInventoryId = PlayerInventoryId, // TD_019 Phase 4: Pass PlayerInventoryId to child slots
                CellSize = CellSize,
                Mediator = _mediator,
                ItemTileSet = _itemTileSet,
                // Force slot to take up proper space
                CustomMinimumSize = new Vector2(CellSize + 20, CellSize + 40) // Extra space for title + margins
            };

            // Listen to slot changes
            slotNode.InventoryChanged += OnSlotChanged;

            vbox.AddChild(slotNode);
            _slotNodes[slot] = slotNode;

            _logger.LogDebug("Created equipment slot: {Slot}", slot);
        }
    }

    private string GetSlotTitle(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.MainHand => "Main Hand",
        EquipmentSlot.OffHand => "Off Hand",
        EquipmentSlot.Head => "Head",
        EquipmentSlot.Torso => "Torso",
        EquipmentSlot.Legs => "Legs",
        _ => slot.ToString()
    };

    private void OnSlotChanged()
    {
        _logger.LogDebug("Equipment slot changed, refreshing all slots");

        // Refresh all slots (re-query equipment state)
        RefreshDisplay(); // async void - no need for _ assignment

        // Notify parent controller (for cross-container refresh)
        EmitSignal(SignalName.InventoryChanged);
    }
}
