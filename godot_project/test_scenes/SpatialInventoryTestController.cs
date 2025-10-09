using System;
using System.Linq;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Entities;
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
    private IActorRepository _actorRepo = null!;

    // Test actor IDs
    // VS_032 Phase 4: Consolidated model - player has backpack + equipment, enemy has separate loot
    private ActorId _playerActorId = ActorId.NewId();      // Player: has backpack + equipment
    private ActorId _enemyLootActorId = ActorId.NewId();   // Enemy/Chest: separate inventory for cross-actor testing

    // Container references (for cross-container refresh)
    // VS_032 Phase 4: Realistic model - player backpack + equipment, enemy loot
    private Components.Inventory.InventoryContainerNode? _playerBackpackNode;
    private Components.Inventory.InventoryContainerNode? _enemyLootNode;
    private Components.Inventory.EquipmentPanelNode? _equipmentPanel;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve dependencies via ServiceLocator
        var mediatorResult = ServiceLocator.GetService<IMediator>();
        var loggerResult = ServiceLocator.GetService<ILogger<SpatialInventoryTestController>>();
        var inventoryRepoResult = ServiceLocator.GetService<IInventoryRepository>();
        var actorRepoResult = ServiceLocator.GetService<IActorRepository>();

        if (mediatorResult.IsFailure || loggerResult.IsFailure ||
            inventoryRepoResult.IsFailure || actorRepoResult.IsFailure)
        {
            GD.PrintErr("[SpatialInventoryTestController] Failed to resolve dependencies");
            return;
        }

        _mediator = mediatorResult.Value;
        _logger = loggerResult.Value;
        _inventoryRepo = inventoryRepoResult.Value;
        _actorRepo = actorRepoResult.Value;

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

    public ActorId GetPlayerActorId() => _playerActorId;
    public ActorId GetEnemyLootActorId() => _enemyLootActorId;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE METHODS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async System.Threading.Tasks.Task InitializeInventories()
    {
        // VS_032 Phase 4: Consolidated actor model
        // Player has: backpack (inventory) + equipment
        // Enemy has: loot (inventory only, for cross-actor item transfer testing)

        // Create player backpack: 10×6 grid (60 capacity)
        var playerBackpack = Darklands.Core.Features.Inventory.Domain.Inventory.Create(
            InventoryId.NewId(),
            gridWidth: 10,
            gridHeight: 6,
            ContainerType.General);

        // Create enemy loot: 8×8 grid (64 capacity)
        var enemyLoot = Darklands.Core.Features.Inventory.Domain.Inventory.Create(
            InventoryId.NewId(),
            gridWidth: 8,
            gridHeight: 8,
            ContainerType.General);

        // Create player actor (has both backpack + equipment)
        var playerActor = new Actor(
            _playerActorId,
            "test_player"  // nameKey for i18n (not used in test scene)
        );

        await _actorRepo.AddActorAsync(playerActor);
        _logger.LogInformation("Created player actor {ActorId} (has backpack + equipment)", _playerActorId);

        // Register inventories with ActorIds
        var repo = (Darklands.Core.Features.Inventory.Infrastructure.InMemoryInventoryRepository)_inventoryRepo;
        repo.RegisterInventoryForActor(_playerActorId, playerBackpack.Value);     // Player's backpack
        repo.RegisterInventoryForActor(_enemyLootActorId, enemyLoot.Value);      // Enemy loot (separate actor)

        _logger.LogInformation("Inventories initialized: Player backpack (10×6), Enemy loot (8×8)");

        await System.Threading.Tasks.Task.CompletedTask;
    }

    private async System.Threading.Tasks.Task PopulateTestItems()
    {
        // WHY: Pre-populate containers with test items for drag-drop validation
        // Phase 1 acceptance criteria require manual drag-drop testing

        // Query all available item types (4 distinct types for color coding)
        _logger.LogDebug("Querying items for test population...");

        var weaponQuery = new GetItemsByTypeQuery("weapon");
        var weaponResult = await _mediator.Send(weaponQuery);

        var consumableQuery = new GetItemsByTypeQuery("consumable");
        var consumableResult = await _mediator.Send(consumableQuery);

        var toolQuery = new GetItemsByTypeQuery("tool");
        var toolResult = await _mediator.Send(toolQuery);

        var armorQuery = new GetItemsByTypeQuery("armor");
        var armorResult = await _mediator.Send(armorQuery);

        if (weaponResult.IsFailure || consumableResult.IsFailure ||
            toolResult.IsFailure || armorResult.IsFailure)
        {
            _logger.LogError("Failed to query items");
            return;
        }

        _logger.LogInformation("Found {WeaponCount} weapons, {ConsumableCount} consumables, {ToolCount} tools, {ArmorCount} armor",
            weaponResult.Value.Count,
            consumableResult.Value.Count,
            toolResult.Value.Count,
            armorResult.Value.Count);

        // Place items in Player Backpack (2 weapons for equipment testing)
        if (weaponResult.Value.Count >= 2)
        {
            await PlaceItemAt(_playerActorId, weaponResult.Value[0].Id, 0, 0, "weapon1");
            await PlaceItemAt(_playerActorId, weaponResult.Value[1].Id, 2, 0, "weapon2");
        }

        // Place items in Enemy Loot (variety of types for cross-actor transfer testing)
        var placements = new[]
        {
            (consumableResult.Value.Count > 0, consumableResult.Value.ElementAtOrDefault(0)?.Id, 0, 0, "consumable"),
            (toolResult.Value.Count > 0, toolResult.Value.ElementAtOrDefault(0)?.Id, 3, 0, "tool"),
            (armorResult.Value.Count > 0, armorResult.Value.ElementAtOrDefault(0)?.Id, 0, 2, "armor")
        };

        foreach (var (hasItem, itemId, x, y, typeName) in placements)
        {
            if (hasItem && itemId != null)
            {
                await PlaceItemAt(_enemyLootActorId, itemId.Value, x, y, typeName);
            }
        }

        _logger.LogInformation("Test item population complete");
    }

    private async System.Threading.Tasks.Task PlaceItemAt(ActorId actorId, ItemId itemId, int x, int y, string itemName)
    {
        _logger.LogDebug("Placing {ItemName} {ItemId} at ({X},{Y})", itemName, itemId, x, y);
        var result = await _mediator.Send(new PlaceItemAtPositionCommand(
            actorId, itemId, new GridPosition(x, y)));

        if (result.IsFailure)
        {
            _logger.LogError("Failed to place {ItemName}: {Error}", itemName, result.Error);
        }
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

        // VS_032 Phase 4: Create Player Backpack (player's main inventory)
        _playerBackpackNode = new Components.Inventory.InventoryContainerNode
        {
            OwnerActorId = _playerActorId,
            ContainerTitle = "Player Backpack",
            CellSize = 48,
            Mediator = _mediator,
            ItemTileSet = ItemTileSet
        };
        _playerBackpackNode.InventoryChanged += OnInventoryChanged;
        backpackAPlaceholder.AddChild(_playerBackpackNode);

        // VS_032 Phase 4: Create Enemy Loot (separate actor for cross-actor testing)
        _enemyLootNode = new Components.Inventory.InventoryContainerNode
        {
            OwnerActorId = _enemyLootActorId,
            ContainerTitle = "Enemy Loot",
            CellSize = 48,
            Mediator = _mediator,
            ItemTileSet = ItemTileSet
        };
        _enemyLootNode.InventoryChanged += OnInventoryChanged;
        backpackBPlaceholder.AddChild(_enemyLootNode);

        // VS_032 Phase 4: Create Equipment Panel (player's equipment - same actor as backpack!)
        _equipmentPanel = new Components.Inventory.EquipmentPanelNode
        {
            OwnerActorId = _playerActorId, // SAME actor as backpack - this is key!
            PanelTitle = "Equipment",
            CellSize = 96,
            Mediator = _mediator,
            ItemTileSet = ItemTileSet
        };
        _equipmentPanel.InventoryChanged += OnInventoryChanged;
        weaponSlotPlaceholder.AddChild(_equipmentPanel);

        _logger.LogInformation("Container nodes attached: Player (backpack + equipment), Enemy (loot)");
    }

    /// <summary>
    /// Called when any container's inventory changes (via signal).
    /// WHY: Refresh ALL containers to sync displays after cross-container moves.
    /// </summary>
    private void OnInventoryChanged()
    {
        _logger.LogDebug("Inventory changed signal received - refreshing all containers");

        // Refresh all container instances
        _playerBackpackNode?.RefreshDisplay();
        _enemyLootNode?.RefreshDisplay();
        // VS_032 Phase 4: Equipment panel refreshes all 5 slots at once
        _equipmentPanel?.RefreshDisplay();
    }
}
