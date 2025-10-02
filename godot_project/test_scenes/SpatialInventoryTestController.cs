using System;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Domain;
using Darklands.Core.Features.Item.Application.Queries;
using Darklands.Core.Infrastructure.DependencyInjection;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands;

/// <summary>
/// Controller for Spatial Inventory Test Scene - demonstrates drag-drop grid inventory (VS_018 Phase 1).
/// </summary>
/// <remarks>
/// PURPOSE:
/// - Manual testing for spatial inventory with drag-drop UX
/// - Validates type filtering (weapon slot rejects potions)
/// - Tests cross-container movement (backpack A → backpack B)
///
/// ARCHITECTURE (ADR-002):
/// - Uses ServiceLocator in _Ready() (Godot constraint)
/// - Commands sent via MediatR (PlaceItemAtPositionCommand, MoveItemBetweenContainersCommand)
/// - Queries for validation (CanPlaceItemAtQuery)
/// </remarks>
public partial class SpatialInventoryTestController : Control
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// TileSet resource containing item sprites.
    /// Assign: res://assets/inventory_ref/item_sprites.tres
    /// </summary>
    [Export] public TileSet? ItemTileSet { get; set; }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IMediator _mediator = null!;
    private ILogger<SpatialInventoryTestController> _logger = null!;
    private IInventoryRepository _inventoryRepo = null!;

    // Test actor IDs (mock player character IDs for inventories)
    private ActorId _backpackAActorId = ActorId.NewId();
    private ActorId _backpackBActorId = ActorId.NewId();
    private ActorId _weaponSlotActorId = ActorId.NewId();

    // Container references (for cross-container refresh)
    private Components.SpatialInventoryContainerNode? _backpackANode;
    private Components.SpatialInventoryContainerNode? _backpackBNode;
    private Components.SpatialInventoryContainerNode? _weaponSlotNode;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve dependencies via ServiceLocator
        var mediatorResult = ServiceLocator.GetService<IMediator>();
        var loggerResult = ServiceLocator.GetService<ILogger<SpatialInventoryTestController>>();
        var repoResult = ServiceLocator.GetService<IInventoryRepository>();

        if (mediatorResult.IsFailure || loggerResult.IsFailure || repoResult.IsFailure)
        {
            GD.PrintErr("[SpatialInventoryTestController] Failed to resolve dependencies");
            return;
        }

        _mediator = mediatorResult.Value;
        _logger = loggerResult.Value;
        _inventoryRepo = repoResult.Value;

        // WHY: ItemTileSet optional for Phase 1 (no sprite rendering yet)
        if (ItemTileSet == null)
        {
            _logger.LogWarning("ItemTileSet not assigned (Phase 1: sprites not rendered)");
        }

        _logger.LogInformation("SpatialInventoryTestController initialized");

        // Initialize inventories, populate items, then attach UI
        // WHY: Async chain ensures inventories registered before item placement
        InitializeAndPopulateAsync();
    }

    private async void InitializeAndPopulateAsync()
    {
        // Step 1: Register inventories with ActorIds
        await InitializeInventories();

        // Step 2: Populate test items (requires inventories to exist)
        await PopulateTestItems();

        // Step 3: Attach UI nodes (requires items to be loaded)
        AttachContainerNodes();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PUBLIC API (for child nodes)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public IMediator GetMediator() => _mediator;
    public TileSet? GetItemTileSet() => ItemTileSet;

    public ActorId GetBackpackAActorId() => _backpackAActorId;
    public ActorId GetBackpackBActorId() => _backpackBActorId;
    public ActorId GetWeaponSlotActorId() => _weaponSlotActorId;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async System.Threading.Tasks.Task InitializeInventories()
    {
        // WHY: Explicitly create inventories with correct grid dimensions
        // Auto-creation uses DefaultCapacity=20, which maps to wrong dimensions

        // Backpack A: 10×6 grid (60 capacity)
        var backpackA = Darklands.Core.Features.Inventory.Domain.Inventory.Create(
            InventoryId.NewId(),
            gridWidth: 10,
            gridHeight: 6,
            ContainerType.General).Value;

        // Backpack B: 8×8 grid (64 capacity)
        var backpackB = Darklands.Core.Features.Inventory.Domain.Inventory.Create(
            InventoryId.NewId(),
            gridWidth: 8,
            gridHeight: 8,
            ContainerType.General).Value;

        // Weapon Slot: 1×1 grid (1 capacity, weapon-only)
        // WHY: Single-slot design matches reference image (Resident Evil style)
        var weaponSlot = Darklands.Core.Features.Inventory.Domain.Inventory.Create(
            InventoryId.NewId(),
            gridWidth: 1,
            gridHeight: 1,
            ContainerType.WeaponOnly).Value;

        // Register inventories with ActorIds
        // WHY: Cast to InMemoryInventoryRepository to access RegisterInventoryForActor
        // (Not part of IInventoryRepository interface - test-only method)
        var repo = (Darklands.Core.Features.Inventory.Infrastructure.InMemoryInventoryRepository)_inventoryRepo;
        repo.RegisterInventoryForActor(_backpackAActorId, backpackA);
        repo.RegisterInventoryForActor(_backpackBActorId, backpackB);
        repo.RegisterInventoryForActor(_weaponSlotActorId, weaponSlot);

        _logger.LogInformation("Inventories initialized with correct grid dimensions");

        // WHY: Await ensures registration completes before returning
        await System.Threading.Tasks.Task.CompletedTask;
    }

    private async System.Threading.Tasks.Task PopulateTestItems()
    {
        // WHY: Pre-populate containers with test items for drag-drop validation
        // Phase 1 acceptance criteria require manual drag-drop testing

        // Query all available items
        _logger.LogDebug("Querying items for test population...");

        var weaponQuery = new GetItemsByTypeQuery("weapon");
        var weaponResult = await _mediator.Send(weaponQuery);

        // WHY: Use "item" type instead of "consumable" (TileSet uses generic "item" for non-weapons)
        var itemQuery = new GetItemsByTypeQuery("item");
        var itemResult = await _mediator.Send(itemQuery);

        if (weaponResult.IsFailure)
        {
            _logger.LogError("Failed to query weapons: {Error}", weaponResult.Error);
            return;
        }

        if (itemResult.IsFailure)
        {
            _logger.LogError("Failed to query items: {Error}", itemResult.Error);
            return;
        }

        _logger.LogInformation("Found {WeaponCount} weapons and {ItemCount} items",
            weaponResult.Value.Count,
            itemResult.Value.Count);

        // Place 2 weapons in Backpack A (using DIFFERENT weapons to avoid collision)
        if (weaponResult.Value.Count >= 2)
        {
            var weapon1 = weaponResult.Value[0].Id;
            var weapon2 = weaponResult.Value[1].Id;

            _logger.LogDebug("Placing weapon1 {ItemId} at Backpack A (0,0)", weapon1);
            var result1 = await _mediator.Send(new PlaceItemAtPositionCommand(
                _backpackAActorId, weapon1, new GridPosition(0, 0)));

            if (result1.IsFailure)
            {
                _logger.LogError("Failed to place weapon1: {Error}", result1.Error);
            }

            _logger.LogDebug("Placing weapon2 {ItemId} at Backpack A (2,0)", weapon2);
            var result2 = await _mediator.Send(new PlaceItemAtPositionCommand(
                _backpackAActorId, weapon2, new GridPosition(2, 0)));

            if (result2.IsFailure)
            {
                _logger.LogError("Failed to place weapon2: {Error}", result2.Error);
            }
        }
        else
        {
            _logger.LogWarning("Not enough weapons found (need 2, got {Count})", weaponResult.Value.Count);
        }

        // Place 2 items in Backpack B (using DIFFERENT items - potions, scrolls, etc.)
        if (itemResult.Value.Count >= 2)
        {
            var item1 = itemResult.Value[0].Id;
            var item2 = itemResult.Value[1].Id;

            _logger.LogDebug("Placing item1 {ItemId} at Backpack B (0,0)", item1);
            var result3 = await _mediator.Send(new PlaceItemAtPositionCommand(
                _backpackBActorId, item1, new GridPosition(0, 0)));

            if (result3.IsFailure)
            {
                _logger.LogError("Failed to place item1: {Error}", result3.Error);
            }

            _logger.LogDebug("Placing item2 {ItemId} at Backpack B (3,0)", item2);
            var result4 = await _mediator.Send(new PlaceItemAtPositionCommand(
                _backpackBActorId, item2, new GridPosition(3, 0)));

            if (result4.IsFailure)
            {
                _logger.LogError("Failed to place item2: {Error}", result4.Error);
            }
        }
        else
        {
            _logger.LogWarning("Not enough items found (need 2, got {Count})", itemResult.Value.Count);
        }

        _logger.LogInformation("Test item population complete");
    }

    /// <summary>
    /// Spawns a test item for dragging (called by item palette buttons).
    /// </summary>
    public async void SpawnTestItem(string itemType)
    {
        // Query for an item of the requested type
        var query = new GetItemsByTypeQuery(itemType);
        var result = await _mediator.Send(query);

        if (result.IsFailure || result.Value.Count == 0)
        {
            _logger.LogWarning("No items found for type: {Type}", itemType);
            return;
        }

        var itemDto = result.Value[0]; // Use first matching item
        _logger.LogInformation("Spawned test item: {Name} (Type: {Type})", itemDto.Name, itemDto.Type);

        // TODO: Create draggable item node at cursor position
        // For now, just log - drag-drop implementation will handle this
    }

    private void AttachContainerNodes()
    {
        // Find container placeholders in scene tree
        var backpackAPlaceholder = GetNode<Control>("VBoxContainer/ContainersRow/BackpackA");
        var backpackBPlaceholder = GetNode<Control>("VBoxContainer/ContainersRow/BackpackB");
        var weaponSlotPlaceholder = GetNode<Control>("VBoxContainer/ContainersRow/WeaponSlot");

        // Create and attach Backpack A container
        _backpackANode = new Components.SpatialInventoryContainerNode
        {
            OwnerActorId = _backpackAActorId,
            ContainerTitle = "Backpack A",
            CellSize = 48,
            Mediator = _mediator,
            ItemTileSet = ItemTileSet
        };
        _backpackANode.InventoryChanged += OnInventoryChanged;
        backpackAPlaceholder.AddChild(_backpackANode);

        // Create and attach Backpack B container
        _backpackBNode = new Components.SpatialInventoryContainerNode
        {
            OwnerActorId = _backpackBActorId,
            ContainerTitle = "Backpack B",
            CellSize = 48,
            Mediator = _mediator,
            ItemTileSet = ItemTileSet
        };
        _backpackBNode.InventoryChanged += OnInventoryChanged;
        backpackBPlaceholder.AddChild(_backpackBNode);

        // Create and attach Weapon Slot (1×1 spatial grid with type filter)
        // WHY: Reuse working SpatialInventoryContainerNode instead of debugging EquipmentSlotNode
        _weaponSlotNode = new Components.SpatialInventoryContainerNode
        {
            OwnerActorId = _weaponSlotActorId,
            ContainerTitle = "Weapon Slot",
            CellSize = 96, // Larger cell for weapon display
            Mediator = _mediator,
            ItemTileSet = ItemTileSet
        };
        _weaponSlotNode.InventoryChanged += OnInventoryChanged;
        weaponSlotPlaceholder.AddChild(_weaponSlotNode);

        _logger.LogInformation("Container nodes attached to scene");
    }

    /// <summary>
    /// Called when any container's inventory changes (via signal).
    /// WHY: Refresh ALL containers to sync displays after cross-container moves.
    /// </summary>
    private void OnInventoryChanged()
    {
        _logger.LogDebug("Inventory changed signal received - refreshing all containers");

        // Refresh all SpatialInventoryContainerNode instances
        _backpackANode?.RefreshDisplay();
        _backpackBNode?.RefreshDisplay();
        _weaponSlotNode?.RefreshDisplay();
    }
}
