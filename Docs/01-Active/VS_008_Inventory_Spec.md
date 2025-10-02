# VS_008: Slot-Based Inventory System (MVP)

**Status**: Tech Lead Review (Awaiting Approval)
**Created**: 2025-10-01 22:30
**Owner**: Tech Lead → Dev Engineer (after approval)
**Size**: M (5-6.5 hours across 4 phases)
**Priority**: Important (Core mechanic, parallel with movement work)
**Depends On**: None (ActorId already exists)

---

## 🎯 Ultra-Think: Architectural Foundation

### Core Insight: "Inventory as ID Container"

The Unity reference couples Item properties (sprite, name, damage) with spatial placement. This violates Clean Architecture - rendering and domain logic mixed.

**Our Approach: Radical Separation**
```
Inventory Feature: Manages ItemId references (add/remove, capacity, queries)
Item Feature:      Defines Item entities (name, type, properties) [Future VS]
Presentation:      Joins data (InventoryId + ItemId → display info)
```

**Why This Is Elegant:**
- ✅ **Single Responsibility**: Inventory = "list with capacity constraint"
- ✅ **Domain Independence**: Inventory doesn't care if Item is a sword or potion
- ✅ **Testability**: No mocks needed - just create ItemId.NewId()
- ✅ **Parallel Development**: Item feature can evolve separately

`✶ Insight ─────────────────────────────────────`
**The Unity reference stores Items (complex objects).
We store ItemIds (simple identifiers).**

This is the core architectural insight that makes our implementation simpler, testable, and scalable. When Combat needs "wielded weapon," it stores ItemId. When UI needs to display an item, it queries Item feature by ItemId.

**No coupling. Pure composition.**
`─────────────────────────────────────────────────`

---

## 📋 What & Why

### What
Slot-based inventory system (20-slot backpack) with add/remove operations, capacity enforcement, and basic UI panel.

### Why
- **Core Mechanic**: Loot management is fundamental to roguelikes
- **Foundation**: Equipment, crafting, and trading all depend on inventory
- **Parallel Development**: Zero conflicts with VS_006/007 (Movement systems)
- **MVP Philosophy**: Simplest inventory that provides value (defer tetris complexity)

### Why NOT Spatial/Tetris Grid Yet?
- ❌ **Unproven Need**: No playtesting data showing players want spatial puzzles
- ❌ **Complexity**: Tetris adds 2-3x development time (shapes, collision, rotation)
- ❌ **YAGNI**: Slot-based inventory might be sufficient (see Shattered Pixel Dungeon)

**Decision Point**: After VS_008 playtesting, evaluate if spatial grid adds value. If yes → VS_013 (Tetris Upgrade). If no → ship slot-based, focus elsewhere.

---

## 🏗️ Architecture: Four-Phase Implementation

### Phase 1: Domain Layer (Pure C#, Zero Dependencies)

**New Files:**

#### `Domain/Common/ItemId.cs` (NEW - Shared Primitive)
```csharp
namespace Darklands.Core.Domain.Common;

/// <summary>
/// Uniquely identifies an item in the game world.
/// Value type with value semantics - two ItemIds with the same Guid are equal.
/// </summary>
/// <remarks>
/// ItemId is a shared primitive used across multiple features:
/// - Inventory (stores ItemId references)
/// - Combat (tracks wielded weapon ItemId)
/// - Loot (identifies dropped items)
/// - Crafting (recipe inputs/outputs)
/// </remarks>
public readonly record struct ItemId(Guid Value)
{
    /// <summary>
    /// Creates a new unique ItemId.
    /// </summary>
    public static ItemId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an ItemId from a string representation.
    /// </summary>
    public static ItemId From(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Returns the string representation of this ItemId.
    /// </summary>
    public override string ToString() => Value.ToString();
}
```

#### `Features/Inventory/Domain/InventoryId.cs`
```csharp
namespace Darklands.Core.Features.Inventory.Domain;

/// <summary>
/// Uniquely identifies an inventory instance.
/// </summary>
public readonly record struct InventoryId(Guid Value)
{
    public static InventoryId NewId() => new(Guid.NewGuid());
    public static InventoryId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
```

#### `Features/Inventory/Domain/Inventory.cs` (Core Domain Entity)
```csharp
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Inventory.Domain;

/// <summary>
/// Represents a slot-based inventory with fixed capacity.
/// </summary>
/// <remarks>
/// ARCHITECTURE: Inventory stores ItemId references, NOT Item entities.
/// This creates clean separation:
/// - Inventory Feature: Container logic (add/remove/query)
/// - Item Feature: Item properties (name, type, weight) [Future VS]
/// - Presentation: Joins data for display
///
/// WHY: Single Responsibility + Testability + Parallel Development
/// </remarks>
public sealed class Inventory
{
    private readonly List<ItemId> _items;

    /// <summary>
    /// Unique identifier for this inventory.
    /// </summary>
    public InventoryId Id { get; private init; }

    /// <summary>
    /// Maximum number of items this inventory can hold.
    /// </summary>
    public int Capacity { get; private init; }

    /// <summary>
    /// Current number of items in inventory.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// True if inventory has reached capacity.
    /// </summary>
    public bool IsFull => Count >= Capacity;

    /// <summary>
    /// Read-only view of items in this inventory.
    /// </summary>
    public IReadOnlyList<ItemId> Items => _items.AsReadOnly();

    private Inventory(InventoryId id, int capacity)
    {
        Id = id;
        Capacity = capacity;
        _items = new List<ItemId>(capacity);
    }

    /// <summary>
    /// Creates a new inventory with specified capacity.
    /// </summary>
    /// <param name="id">Unique inventory identifier</param>
    /// <param name="capacity">Maximum number of items (must be positive)</param>
    /// <returns>Result containing new Inventory or error message</returns>
    public static Result<Inventory> Create(InventoryId id, int capacity)
    {
        if (capacity <= 0)
            return Result.Failure<Inventory>("Capacity must be positive");

        if (capacity > 100)
            return Result.Failure<Inventory>("Capacity cannot exceed 100");

        return Result.Success(new Inventory(id, capacity));
    }

    /// <summary>
    /// Adds an item to this inventory.
    /// </summary>
    /// <param name="itemId">ID of item to add</param>
    /// <returns>Success if added, Failure with reason if not</returns>
    public Result AddItem(ItemId itemId)
    {
        // BUSINESS RULE: Cannot add to full inventory
        if (IsFull)
            return Result.Failure("Inventory is full");

        // BUSINESS RULE: Cannot add duplicate items (items are unique)
        if (_items.Contains(itemId))
            return Result.Failure("Item already in inventory");

        _items.Add(itemId);
        return Result.Success();
    }

    /// <summary>
    /// Removes an item from this inventory.
    /// </summary>
    /// <param name="itemId">ID of item to remove</param>
    /// <returns>Success if removed, Failure if not found</returns>
    public Result RemoveItem(ItemId itemId)
    {
        if (!_items.Contains(itemId))
            return Result.Failure("Item not found in inventory");

        _items.Remove(itemId);
        return Result.Success();
    }

    /// <summary>
    /// Checks if this inventory contains an item.
    /// </summary>
    public bool Contains(ItemId itemId) => _items.Contains(itemId);

    /// <summary>
    /// Removes all items from this inventory.
    /// </summary>
    public void Clear() => _items.Clear();
}
```

**Tests (Phase 1):**
```csharp
// tests/Darklands.Core.Tests/Features/Inventory/Domain/InventoryTests.cs

[Trait("Category", "Phase1")]
public class InventoryTests
{
    [Fact]
    public void Create_WithValidCapacity_ShouldSucceed()
    {
        // WHEN creating inventory with capacity 20
        var result = Inventory.Create(InventoryId.NewId(), capacity: 20);

        // THEN succeeds and has correct properties
        result.IsSuccess.Should().BeTrue();
        result.Value.Capacity.Should().Be(20);
        result.Value.Count.Should().Be(0);
        result.Value.IsFull.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidCapacity_ShouldFail(int invalidCapacity)
    {
        // WHEN creating inventory with invalid capacity
        var result = Inventory.Create(InventoryId.NewId(), invalidCapacity);

        // THEN fails with descriptive error
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("positive");
    }

    [Fact]
    public void AddItem_WhenNotFull_ShouldSucceed()
    {
        // GIVEN inventory with capacity 3
        var inv = Inventory.Create(InventoryId.NewId(), capacity: 3).Value;
        var itemId = ItemId.NewId();

        // WHEN adding item
        var result = inv.AddItem(itemId);

        // THEN succeeds and contains item
        result.IsSuccess.Should().BeTrue();
        inv.Contains(itemId).Should().BeTrue();
        inv.Count.Should().Be(1);
    }

    [Fact]
    public void AddItem_WhenFull_ShouldFail()
    {
        // GIVEN full inventory
        var inv = Inventory.Create(InventoryId.NewId(), capacity: 2).Value;
        inv.AddItem(ItemId.NewId());
        inv.AddItem(ItemId.NewId());

        // WHEN adding third item
        var result = inv.AddItem(ItemId.NewId());

        // THEN fails with descriptive error
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("full");
    }

    [Fact]
    public void AddItem_WhenDuplicateItem_ShouldFail()
    {
        // BUSINESS RULE: Items are unique, cannot add same item twice

        // GIVEN inventory with item
        var inv = Inventory.Create(InventoryId.NewId(), capacity: 5).Value;
        var itemId = ItemId.NewId();
        inv.AddItem(itemId);

        // WHEN adding same item again
        var result = inv.AddItem(itemId);

        // THEN fails with descriptive error
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already in inventory");
    }

    [Fact]
    public void RemoveItem_WhenExists_ShouldSucceed()
    {
        // GIVEN inventory with item
        var inv = Inventory.Create(InventoryId.NewId(), capacity: 5).Value;
        var itemId = ItemId.NewId();
        inv.AddItem(itemId);

        // WHEN removing item
        var result = inv.RemoveItem(itemId);

        // THEN succeeds and no longer contains item
        result.IsSuccess.Should().BeTrue();
        inv.Contains(itemId).Should().BeFalse();
        inv.Count.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_WhenNotExists_ShouldFail()
    {
        // GIVEN empty inventory
        var inv = Inventory.Create(InventoryId.NewId(), capacity: 5).Value;

        // WHEN removing non-existent item
        var result = inv.RemoveItem(ItemId.NewId());

        // THEN fails with descriptive error
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void IsFull_WhenAtCapacity_ShouldBeTrue()
    {
        // GIVEN inventory at capacity
        var inv = Inventory.Create(InventoryId.NewId(), capacity: 2).Value;
        inv.AddItem(ItemId.NewId());
        inv.AddItem(ItemId.NewId());

        // THEN IsFull is true
        inv.IsFull.Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // GIVEN inventory with 3 items
        var inv = Inventory.Create(InventoryId.NewId(), capacity: 5).Value;
        inv.AddItem(ItemId.NewId());
        inv.AddItem(ItemId.NewId());
        inv.AddItem(ItemId.NewId());

        // WHEN clearing
        inv.Clear();

        // THEN all items removed
        inv.Count.Should().Be(0);
        inv.Items.Should().BeEmpty();
    }
}
```

**Estimated Time**: 1.5-2 hours (10 tests, ~300 lines)

---

### Phase 2: Application Layer (Commands & Queries)

**New Files:**

#### Commands
```csharp
// Features/Inventory/Application/Commands/AddItemCommand.cs
using MediatR;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to add an item to an actor's inventory.
/// </summary>
/// <param name="ActorId">Actor whose inventory will receive the item</param>
/// <param name="ItemId">Item to add</param>
public record AddItemCommand(ActorId ActorId, ItemId ItemId) : IRequest<Result>;

// Features/Inventory/Application/Commands/AddItemCommandHandler.cs
public class AddItemCommandHandler : IRequestHandler<AddItemCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<AddItemCommandHandler> _logger;

    public AddItemCommandHandler(
        IInventoryRepository inventories,
        ILogger<AddItemCommandHandler> logger)
    {
        _inventories = inventories;
        _logger = logger;
    }

    public async Task<Result> Handle(AddItemCommand cmd, CancellationToken ct)
    {
        _logger.LogDebug("Adding item {ItemId} to {ActorId}'s inventory",
            cmd.ItemId, cmd.ActorId);

        return await _inventories.GetByActorIdAsync(cmd.ActorId)
            .Bind(inv => inv.AddItem(cmd.ItemId)
                .Tap(() => _inventories.SaveAsync(inv, ct)));
    }
}

// Features/Inventory/Application/Commands/RemoveItemCommand.cs
public record RemoveItemCommand(ActorId ActorId, ItemId ItemId) : IRequest<Result>;

// Features/Inventory/Application/Commands/RemoveItemCommandHandler.cs
// Similar structure to AddItemCommandHandler
```

#### Queries
```csharp
// Features/Inventory/Application/Queries/GetInventoryQuery.cs
using MediatR;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to retrieve an actor's inventory.
/// </summary>
public record GetInventoryQuery(ActorId ActorId) : IRequest<Result<InventoryDto>>;

// Features/Inventory/Application/Queries/InventoryDto.cs
/// <summary>
/// Data Transfer Object for inventory state.
/// </summary>
/// <remarks>
/// DTOs prevent presentation layer from directly accessing domain entities.
/// Changes to Inventory entity don't break UI code.
/// </remarks>
public record InventoryDto(
    InventoryId InventoryId,
    ActorId ActorId,
    int Capacity,
    int Count,
    bool IsFull,
    IReadOnlyList<ItemId> Items);

// Features/Inventory/Application/Queries/GetInventoryQueryHandler.cs
public class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, Result<InventoryDto>>
{
    private readonly IInventoryRepository _inventories;

    public GetInventoryQueryHandler(IInventoryRepository inventories)
    {
        _inventories = inventories;
    }

    public async Task<Result<InventoryDto>> Handle(
        GetInventoryQuery query,
        CancellationToken ct)
    {
        return await _inventories.GetByActorIdAsync(query.ActorId)
            .Map(inv => new InventoryDto(
                inv.Id,
                query.ActorId,
                inv.Capacity,
                inv.Count,
                inv.IsFull,
                inv.Items));
    }
}
```

**Tests (Phase 2):**
```csharp
[Trait("Category", "Phase2")]
public class AddItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidItem_ShouldAddToInventory()
    {
        // GIVEN actor with inventory
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var handler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);

        // WHEN adding item
        var result = await handler.Handle(new AddItemCommand(actorId, itemId), default);

        // THEN succeeds and inventory contains item
        result.IsSuccess.Should().BeTrue();
        var inventory = await repository.GetByActorIdAsync(actorId);
        inventory.Value.Contains(itemId).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FullInventory_ShouldFail()
    {
        // GIVEN actor with full inventory (capacity 2)
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var handler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);

        // Fill inventory
        await handler.Handle(new AddItemCommand(actorId, ItemId.NewId()), default);
        await handler.Handle(new AddItemCommand(actorId, ItemId.NewId()), default);

        // WHEN adding third item
        var result = await handler.Handle(new AddItemCommand(actorId, ItemId.NewId()), default);

        // THEN fails
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("full");
    }
}

[Trait("Category", "Phase2")]
public class GetInventoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingInventory_ShouldReturnDto()
    {
        // GIVEN actor with 2 items
        var actorId = ActorId.NewId();
        var item1 = ItemId.NewId();
        var item2 = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var addHandler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);

        await addHandler.Handle(new AddItemCommand(actorId, item1), default);
        await addHandler.Handle(new AddItemCommand(actorId, item2), default);

        // WHEN querying inventory
        var queryHandler = new GetInventoryQueryHandler(repository);
        var result = await queryHandler.Handle(new GetInventoryQuery(actorId), default);

        // THEN returns correct DTO
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Count.Should().Be(2);
        dto.Items.Should().Contain(new[] { item1, item2 });
    }
}
```

**Estimated Time**: 1-1.5 hours (6 tests, ~255 lines)

---

### Phase 3: Infrastructure Layer (Repository & State Management)

**New Files:**

#### Repository Interface
```csharp
// Features/Inventory/Infrastructure/IInventoryRepository.cs
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Domain;

namespace Darklands.Core.Features.Inventory.Infrastructure;

/// <summary>
/// Repository for managing inventory persistence.
/// </summary>
/// <remarks>
/// ASYNC DESIGN: Methods are async even though in-memory implementation is synchronous.
/// This future-proofs for SQLite/JSON persistence without changing consumers.
/// </remarks>
public interface IInventoryRepository
{
    /// <summary>
    /// Retrieves an actor's inventory. Auto-creates if doesn't exist.
    /// </summary>
    Task<Result<Domain.Inventory>> GetByActorIdAsync(ActorId actorId, CancellationToken ct = default);

    /// <summary>
    /// Persists inventory changes.
    /// </summary>
    Task<Result> SaveAsync(Domain.Inventory inventory, CancellationToken ct = default);

    /// <summary>
    /// Deletes an inventory.
    /// </summary>
    Task<Result> DeleteAsync(InventoryId inventoryId, CancellationToken ct = default);
}
```

#### In-Memory Implementation
```csharp
// Features/Inventory/Infrastructure/InMemoryInventoryRepository.cs
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Domain;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Infrastructure;

/// <summary>
/// In-memory inventory repository for MVP.
/// </summary>
/// <remarks>
/// THREAD SAFETY: Not thread-safe by design (single-player game, Godot main thread only).
/// PERSISTENCE: State lost on application restart (acceptable for MVP).
/// FUTURE: Replace with SQLite/JSON persistence without changing interface.
/// </remarks>
public class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly Dictionary<ActorId, Domain.Inventory> _inventoriesByActor = new();
    private readonly ILogger<InMemoryInventoryRepository> _logger;
    private const int DefaultCapacity = 20;

    public InMemoryInventoryRepository(ILogger<InMemoryInventoryRepository> logger)
    {
        _logger = logger;
    }

    public Task<Result<Domain.Inventory>> GetByActorIdAsync(
        ActorId actorId,
        CancellationToken ct = default)
    {
        if (!_inventoriesByActor.TryGetValue(actorId, out var inventory))
        {
            // DESIGN DECISION: Auto-create inventory with default capacity
            // WHY: Every actor needs an inventory. Explicit creation would add boilerplate.
            // ALTERNATIVE: Require explicit CreateInventoryCommand (more ceremony, no clear benefit for MVP)

            _logger.LogDebug("Auto-creating inventory for {ActorId} with capacity {Capacity}",
                actorId, DefaultCapacity);

            inventory = Domain.Inventory.Create(InventoryId.NewId(), DefaultCapacity).Value;
            _inventoriesByActor[actorId] = inventory;
        }

        return Task.FromResult(Result.Success(inventory));
    }

    public Task<Result> SaveAsync(Domain.Inventory inventory, CancellationToken ct = default)
    {
        // In-memory: No-op (already in dictionary, passed by reference)
        // Future implementations: Write to SQLite/JSON here
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteAsync(InventoryId inventoryId, CancellationToken ct = default)
    {
        var toRemove = _inventoriesByActor.FirstOrDefault(kvp => kvp.Value.Id == inventoryId);
        if (toRemove.Key != default)
        {
            _inventoriesByActor.Remove(toRemove.Key);
            _logger.LogDebug("Deleted inventory {InventoryId}", inventoryId);
        }
        return Task.FromResult(Result.Success());
    }
}
```

#### Dependency Injection Registration
```csharp
// Infrastructure/DependencyInjection/InventoryModule.cs
public static class InventoryModule
{
    public static IServiceCollection AddInventoryServices(this IServiceCollection services)
    {
        // Repository
        services.AddSingleton<IInventoryRepository, InMemoryInventoryRepository>();

        // MediatR handlers auto-registered by assembly scanning

        return services;
    }
}
```

**Tests (Phase 3):**
```csharp
[Trait("Category", "Phase3")]
public class InMemoryInventoryRepositoryTests
{
    [Fact]
    public async Task GetByActorId_FirstTime_ShouldAutoCreateInventory()
    {
        // GIVEN new actor (no inventory yet)
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // WHEN getting inventory first time
        var result = await repository.GetByActorIdAsync(actorId);

        // THEN auto-creates with default capacity
        result.IsSuccess.Should().BeTrue();
        result.Value.Capacity.Should().Be(20);
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetByActorId_SecondTime_ShouldReturnSameInventory()
    {
        // GIVEN actor with inventory containing item
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        var inv1 = await repository.GetByActorIdAsync(actorId);
        inv1.Value.AddItem(itemId);

        // WHEN getting inventory again
        var inv2 = await repository.GetByActorIdAsync(actorId);

        // THEN returns same instance with item
        inv2.Value.Contains(itemId).Should().BeTrue();
        inv2.Value.Id.Should().Be(inv1.Value.Id);
    }
}
```

**Estimated Time**: 1 hour (4 tests, ~185 lines)

---

### Phase 4: Presentation Layer (Godot UI)

**New Files:**

#### UI Controller Node
```csharp
// godot_project/features/inventory/InventoryPanelNode.cs
using Godot;
using MediatR;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Infrastructure;

namespace Darklands.Features.Inventory;

/// <summary>
/// Godot UI controller for inventory panel.
/// </summary>
/// <remarks>
/// ARCHITECTURE: Uses ServiceLocator ONLY in _Ready() (ADR-002 compliant).
/// Pure reactive UI - queries Core on demand, no state duplication.
/// </remarks>
public partial class InventoryPanelNode : Control
{
    private IMediator _mediator = null!;
    private ActorId _actorId;

    // Child nodes
    private GridContainer _slotsContainer = null!;
    private Label _capacityLabel = null!;
    private Button _addTestItemButton = null!;
    private Button _removeLastItemButton = null!;

    public override void _Ready()
    {
        // ServiceLocator at boundary (ADR-002)
        _mediator = ServiceLocator.Get<IMediator>();

        // Get child nodes
        _slotsContainer = GetNode<GridContainer>("SlotsContainer");
        _capacityLabel = GetNode<Label>("CapacityLabel");
        _addTestItemButton = GetNode<Button>("AddTestItemButton");
        _removeLastItemButton = GetNode<Button>("RemoveLastItemButton");

        // Wire up events
        _addTestItemButton.Pressed += OnAddTestItemPressed;
        _removeLastItemButton.Pressed += OnRemoveLastItemPressed;

        // For MVP: Use player actor (hardcoded)
        // Future: Pass ActorId from parent scene
        _actorId = ActorId.From("00000000-0000-0000-0000-000000000001"); // Player

        // Initial UI refresh
        RefreshUI();
    }

    private async void OnAddTestItemPressed()
    {
        var itemId = ItemId.NewId();
        var result = await _mediator.Send(new AddItemCommand(_actorId, itemId));

        if (result.IsFailure)
        {
            GD.PrintErr($"Failed to add item: {result.Error}");
            ShowErrorPopup(result.Error);
        }
        else
        {
            GD.Print($"Added item {itemId}");
            RefreshUI();
        }
    }

    private async void OnRemoveLastItemPressed()
    {
        var inventoryResult = await _mediator.Send(new GetInventoryQuery(_actorId));
        if (inventoryResult.IsFailure || inventoryResult.Value.Items.Count == 0)
        {
            GD.PrintErr("No items to remove");
            return;
        }

        var lastItemId = inventoryResult.Value.Items[^1];
        var result = await _mediator.Send(new RemoveItemCommand(_actorId, lastItemId));

        if (result.IsFailure)
        {
            GD.PrintErr($"Failed to remove item: {result.Error}");
        }
        else
        {
            GD.Print($"Removed item {lastItemId}");
            RefreshUI();
        }
    }

    private async void RefreshUI()
    {
        var result = await _mediator.Send(new GetInventoryQuery(_actorId));

        if (result.IsFailure)
        {
            GD.PrintErr($"Failed to query inventory: {result.Error}");
            return;
        }

        var inventory = result.Value;

        // Update capacity label
        _capacityLabel.Text = $"Items: {inventory.Count}/{inventory.Capacity}";

        // Update slot visuals
        UpdateSlots(inventory);

        // Update button states
        _addTestItemButton.Disabled = inventory.IsFull;
        _removeLastItemButton.Disabled = inventory.Count == 0;
    }

    private void UpdateSlots(InventoryDto inventory)
    {
        // Clear existing slot visuals
        foreach (var child in _slotsContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Create slot visuals (20 slots)
        for (int i = 0; i < inventory.Capacity; i++)
        {
            var slot = CreateSlotVisual(i < inventory.Count ? inventory.Items[i] : null);
            _slotsContainer.AddChild(slot);
        }
    }

    private Control CreateSlotVisual(ItemId? itemId)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(64, 64)
        };

        var label = new Label
        {
            Text = itemId.HasValue ? "ITEM" : "",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.AddChild(label);
        return panel;
    }

    private void ShowErrorPopup(string message)
    {
        // Simple error display (can be enhanced later)
        var popup = new AcceptDialog
        {
            DialogText = message,
            Title = "Inventory Error"
        };
        AddChild(popup);
        popup.PopupCentered();
    }
}
```

#### Godot Scene Setup
```
InventoryPanel (InventoryPanelNode.cs)
├─ SlotsContainer (GridContainer)
│  └─ [Slots created dynamically]
├─ CapacityLabel (Label)
├─ AddTestItemButton (Button) - "Add Test Item"
└─ RemoveLastItemButton (Button) - "Remove Last"
```

**Manual Testing Checklist:**
- ✅ Click "Add Test Item" → Slot fills, capacity label updates (0/20 → 1/20)
- ✅ Add 20 items → Button disables, capacity shows 20/20
- ✅ Try adding 21st item → Error popup: "Inventory is full"
- ✅ Click "Remove Last" → Slot empties, capacity updates (20/20 → 19/20)
- ✅ Remove all items → "Remove Last" button disables
- ✅ Console shows add/remove logs (debug feedback)

**Estimated Time**: 1.5-2 hours (UI scene + manual testing)

---

## ✅ Done When (Acceptance Criteria)

### Functional Requirements
1. ✅ Actor can add item to inventory via AddItemCommand
2. ✅ Actor can remove item from inventory via RemoveItemCommand
3. ✅ Cannot add item when inventory full (returns "Inventory is full" error)
4. ✅ Cannot add duplicate item (returns "Item already in inventory" error)
5. ✅ Cannot remove non-existent item (returns "Item not found" error)
6. ✅ UI panel displays 20 inventory slots
7. ✅ UI updates when item added/removed (capacity label, slot visuals, button states)
8. ✅ Query inventory returns list of ItemIds with correct count

### Architecture Requirements
9. ✅ Architecture tests pass (zero Godot dependencies in Darklands.Core)
10. ✅ ItemId added to Domain/Common (shared primitive)
11. ✅ All Phase 1-3 tests pass (<100ms unit tests, >90% coverage)
12. ✅ ServiceLocator used ONLY in _Ready() (ADR-002 compliant)
13. ✅ Result<T> used for all operations that can fail (ADR-003)
14. ✅ No events in MVP (deferred until cross-feature needs emerge)

### Manual Testing
15. ✅ Add 20 items → All slots filled → Button disables
16. ✅ Try adding 21st item → Error popup: "Inventory is full"
17. ✅ Remove items → Slots empty in correct order
18. ✅ UI responds instantly (<16ms frame time maintained)

---

## 🚫 What We're NOT Building (Scope Control)

**Deferred to Future VSs:**
- ❌ **Item entities** (name, sprite, type, properties) - Separate VS_009: Item Definition System
- ❌ **Equipment slots** (weapon, armor, rings) - Separate VS_010: Equipment System
- ❌ **Drag-and-drop UI** - Polish phase (mouse events, cursor changes)
- ❌ **Item stacking** (5x Potion) - VS_011: Stackable Items
- ❌ **Weight/encumbrance** - VS_012: Weight System (if playtesting shows need)
- ❌ **Spatial grid** (tetris placement) - VS_013: Tetris Inventory (if playtesting shows need)
- ❌ **Loot drops** - Combat feature extension
- ❌ **Item tooltips** - UI polish
- ❌ **Quickslots/hotbar** - Separate VS
- ❌ **Inventory events** (InventoryChangedEvent) - Add when cross-feature needs emerge

**MVP Scope:** Pure "list of ItemIds with capacity constraint" + basic UI to prove it works.

---

## 📊 Size Estimation Summary

| Phase | Files | Lines | Tests | Time |
|-------|-------|-------|-------|------|
| Phase 1: Domain | 3 files | ~300 | 10 tests | 1.5-2h |
| Phase 2: Application | 6 files | ~255 | 6 tests | 1-1.5h |
| Phase 3: Infrastructure | 3 files | ~185 | 4 tests | 1h |
| Phase 4: Presentation | 2 files | ~150 + scene | Manual | 1.5-2h |
| **Total** | **14 files** | **~890 lines** | **20 tests** | **5-6.5h** |

**Complexity: M (Medium)**

---

## 🔗 Dependencies & Conflicts

### Dependencies
- ✅ **ActorId** - Already exists in Domain/Common
- ⚠️ **ItemId** - NEW primitive (add in Phase 1)
- ✅ **MediatR** - Already registered
- ✅ **CSharpFunctionalExtensions** - Already in use

### Conflicts
- ✅ **None!** - Inventory is orthogonal to Movement (VS_006/007)
- ✅ **Parallel Development Approved** - Zero shared code paths

---

## 🎯 Architecture Validation

### ADR-002: Godot Integration Architecture
- ✅ **ItemId.cs, Inventory.cs**: Pure C#, zero Godot dependencies
- ✅ **IInventoryRepository**: Microsoft.Extensions abstractions only
- ✅ **InventoryPanelNode**: ServiceLocator ONLY in _Ready()
- ✅ **Testable**: All business logic in Core (no Godot runtime needed)

### ADR-003: Functional Error Handling
- ✅ **Result<T>** for all operations that can fail
- ✅ **Descriptive errors**: "Inventory is full" vs. generic "false"
- ✅ **No exceptions** for business logic (capacity, duplicates)

### ADR-004: Feature-Based Clean Architecture
- ✅ **ItemId** in Domain/Common (shared by 3+ features: Inventory, Combat, Loot)
- ✅ **Inventory** in Features/Inventory/Domain (feature-specific entity)
- ✅ **No events** for MVP (YAGNI - add when cross-feature needs emerge)
- ✅ **Repository pattern** for future persistence (SQLite/JSON)

---

## 💡 Key Design Decisions (Tech Lead Rationale)

### 1. ItemId as Shared Primitive (Domain/Common)
**Decision**: ItemId in Domain/Common, NOT Features/Inventory/Domain

**Rationale**: ItemId will be referenced by:
- Inventory (stores ItemId list)
- Combat (tracks wielded weapon ItemId)
- Loot (identifies dropped items)
- Crafting (recipe inputs/outputs)

**Rule**: If 3+ features use it → Domain/Common. ItemId qualifies.

---

### 2. Inventory Stores ItemId, NOT Item Entities
**Decision**: `List<ItemId>` instead of `List<Item>`

**Rationale**:
- ✅ **Single Responsibility**: Inventory = "container logic", Item = "item properties"
- ✅ **Testability**: No mocks needed for Item properties in inventory tests
- ✅ **Parallel Development**: Item feature can evolve separately
- ✅ **Performance**: ItemId (Guid) is 16 bytes vs. Item object (potentially KB with strings, sprites)

**Tradeoff**: Presentation must join data (query Item feature by ItemId). Acceptable cost for architectural cleanliness.

---

### 3. No Events in MVP
**Decision**: Defer InventoryChangedEvent until cross-feature needs emerge

**Rationale**:
- Current consumers (UI) can query on-demand (no event needed)
- Future consumers (Achievements, Quests) don't exist yet
- Events add complexity (handler registration, testing, event sourcing considerations)

**When to add**: When a feature needs to REACT to inventory changes WITHOUT being queried. Example: Achievement system listens for "100 items collected."

---

### 4. Explicit Inventory Creation (Player-Controlled Actors Only)
**Decision**: Repository requires explicit `CreateInventoryCommand` (no auto-creation)

**Rationale**:
- **Only player-controlled actors need inventories** (player, party members in multiplayer)
- NPCs/Enemies have equipment slots only (wielded weapon, worn armor) - separate system
- Loot drops are ground items (ItemId at Position) - separate system
- Explicit creation = clear intent ("This actor manages loot")

**Who Gets Inventory:**
- ✅ Player character (created at game start)
- ✅ Player-controlled companions (party members, multiplayer co-op)
- ❌ NPCs/Enemies (they don't carry backpacks! They have equipment slots only)
- ❌ Neutral NPCs (merchants have shop inventory, not personal backpack)

**Future Extension**: Companion system can call `CreateInventoryCommand(companionActorId, 15)` to give party members smaller inventories

**Alternative Considered**: Auto-create for all ActorIds → Rejected (enemies don't need backpacks, wastes memory)

---

### 5. Slot-Based, NOT Tetris Grid
**Decision**: Simple list with capacity constraint (20 slots)

**Rationale**:
- ❌ **Unproven Need**: No playtesting data showing players want spatial puzzles
- ✅ **Shattered Pixel Dungeon Success**: 13M+ downloads with slot-based inventory
- ✅ **MVP Philosophy**: Simplest solution that provides value
- ✅ **Easy Upgrade Path**: Tetris can be added later without breaking changes (ItemId references remain same)

**Decision Point**: After VS_008 playtesting:
- IF players request spatial puzzles → Create VS_013 (Tetris Upgrade)
- IF players satisfied → Ship slot-based, focus development elsewhere

---

## 🚀 Next Steps

### For Product Owner
1. Review VS_008 specification
2. Approve scope and acceptance criteria
3. Confirm priority (Important, parallel with movement work)

### For Tech Lead (Me)
1. ✅ **COMPLETE** - Architecture review and specification complete
2. ⏳ Await Product Owner approval
3. Hand off to Dev Engineer with implementation guide

### For Dev Engineer (After Approval)
1. Create feature branch: `feat/vs-008-slot-inventory`
2. Implement Phase 1 (Domain) → Run tests (`dotnet test --filter "Category=Phase1"`)
3. Implement Phase 2 (Application) → Run tests (`dotnet test --filter "Category=Phase2"`)
4. Implement Phase 3 (Infrastructure) → Run tests (`dotnet test --filter "Category=Phase3"`)
5. Implement Phase 4 (Presentation) → Manual testing in Godot
6. Create PR with phase markers in commits: `feat(inventory): Phase 1 - Domain layer [1/4]`

---

`✶ Insight ─────────────────────────────────────`
**This specification demonstrates "sound and elegant" architecture:**

1. **Sound** = Correct dependencies, testable, maintainable
   - ADR-002/003/004 compliant
   - Zero Godot dependencies in Core
   - Result<T> functional error handling
   - Comprehensive test coverage

2. **Elegant** = Simple, minimal, easy to understand
   - Inventory stores ItemIds (not complex Item objects)
   - 890 lines total (vs. Unity reference's 343 lines JUST for manager)
   - No premature optimization (spatial grid deferred until proven needed)
   - Auto-creation removes boilerplate

**The simplest solution that solves the problem completely.**
`─────────────────────────────────────────────────`

---

**Status**: Ready for Product Owner approval
**Confidence**: High (architecture proven by VS_001/005/006, no technical unknowns)
**Parallel Development**: Approved (zero conflicts with Movement systems)
