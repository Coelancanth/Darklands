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
