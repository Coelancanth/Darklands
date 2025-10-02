using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
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

    // Test actor IDs (mock player character IDs for inventories)
    private ActorId _backpackAActorId = ActorId.NewId();
    private ActorId _backpackBActorId = ActorId.NewId();
    private ActorId _weaponSlotActorId = ActorId.NewId();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        // Resolve dependencies via ServiceLocator
        var mediatorResult = ServiceLocator.GetService<IMediator>();
        var loggerResult = ServiceLocator.GetService<ILogger<SpatialInventoryTestController>>();

        if (mediatorResult.IsFailure || loggerResult.IsFailure)
        {
            GD.PrintErr("[SpatialInventoryTestController] Failed to resolve dependencies");
            return;
        }

        _mediator = mediatorResult.Value;
        _logger = loggerResult.Value;

        if (ItemTileSet == null)
        {
            _logger.LogError("ItemTileSet not assigned in editor");
            return;
        }

        _logger.LogInformation("SpatialInventoryTestController initialized");

        // Initialize inventories (auto-created by repository on first access)
        InitializeInventories();

        // Attach container nodes to scene
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

    private async void InitializeInventories()
    {
        // Query inventories to trigger auto-creation in repository
        await _mediator.Send(new GetInventoryQuery(_backpackAActorId));
        await _mediator.Send(new GetInventoryQuery(_backpackBActorId));
        await _mediator.Send(new GetInventoryQuery(_weaponSlotActorId));

        _logger.LogInformation("Inventories initialized for test actors");
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
        var backpackA = new Components.SpatialInventoryContainerNode
        {
            OwnerActorId = _backpackAActorId,
            ContainerTitle = "Backpack A",
            CellSize = 48
        };
        backpackAPlaceholder.AddChild(backpackA);

        // Create and attach Backpack B container
        var backpackB = new Components.SpatialInventoryContainerNode
        {
            OwnerActorId = _backpackBActorId,
            ContainerTitle = "Backpack B",
            CellSize = 48
        };
        backpackBPlaceholder.AddChild(backpackB);

        // Create and attach Weapon Slot container
        var weaponSlot = new Components.SpatialInventoryContainerNode
        {
            OwnerActorId = _weaponSlotActorId,
            ContainerTitle = "Weapon Slot",
            CellSize = 48
        };
        weaponSlotPlaceholder.AddChild(weaponSlot);

        _logger.LogInformation("Container nodes attached to scene");
    }
}
