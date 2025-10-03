using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Inventory.Domain;

/// <summary>
/// Represents a spatial grid-based inventory with fixed capacity.
/// </summary>
/// <remarks>
/// ARCHITECTURE: Inventory stores ItemId references, NOT Item entities.
/// This creates clean separation:
/// - Inventory Feature: Container logic (add/remove/query/spatial placement)
/// - Item Feature: Item properties (name, type, weight)
/// - Presentation: Joins data for display
///
/// WHY: Single Responsibility + Testability + Parallel Development
///
/// VS_018 EVOLUTION:
/// - VS_008: Simple list-based storage (20-slot backpack)
/// - VS_018 Phase 1: Spatial grid with positions (drag-drop UX)
/// - Dictionary primary storage ensures single source of truth
/// - Items property computed for backward compatibility
/// </remarks>
public sealed class Inventory
{
    private readonly Dictionary<ItemId, GridPosition> _itemPositions;

    /// <summary>
    /// Unique identifier for this inventory.
    /// </summary>
    public InventoryId Id { get; private init; }

    /// <summary>
    /// Maximum number of items this inventory can hold.
    /// </summary>
    public int Capacity { get; private init; }

    /// <summary>
    /// Grid width (number of columns).
    /// </summary>
    public int GridWidth { get; private init; }

    /// <summary>
    /// Grid height (number of rows).
    /// </summary>
    public int GridHeight { get; private init; }

    /// <summary>
    /// Container type determines item acceptance rules (General, WeaponOnly, etc.).
    /// </summary>
    public ContainerType ContainerType { get; private init; }

    /// <summary>
    /// Current number of items in inventory.
    /// </summary>
    public int Count => _itemPositions.Count;

    /// <summary>
    /// True if inventory has reached capacity.
    /// </summary>
    public bool IsFull => Count >= Capacity;

    /// <summary>
    /// Read-only view of items in this inventory.
    /// </summary>
    /// <remarks>
    /// BACKWARD COMPATIBILITY: Computed from Dictionary keys.
    /// VS_008 code uses this property and continues to work unchanged.
    /// </remarks>
    public IReadOnlyList<ItemId> Items => _itemPositions.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Read-only view of item placements (ItemId → GridPosition mapping).
    /// </summary>
    public IReadOnlyDictionary<ItemId, GridPosition> ItemPlacements => _itemPositions;

    private Inventory(
        InventoryId id,
        int gridWidth,
        int gridHeight,
        ContainerType containerType)
    {
        Id = id;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        Capacity = gridWidth * gridHeight;
        ContainerType = containerType;
        _itemPositions = new Dictionary<ItemId, GridPosition>(Capacity);
    }

    /// <summary>
    /// Creates a new inventory with specified capacity (VS_008 signature).
    /// Maps capacity to grid dimensions for backward compatibility.
    /// </summary>
    /// <param name="id">Unique inventory identifier</param>
    /// <param name="capacity">Maximum number of items (must be positive)</param>
    /// <returns>Result containing new Inventory or error message</returns>
    /// <remarks>
    /// BACKWARD COMPATIBILITY: This overload preserves VS_008 behavior.
    /// Algorithm: Square root approximation ensures capacity preserved exactly.
    /// Examples: 20→5×4, 100→10×10, 30→6×5
    /// All inventories default to ContainerType.General.
    /// </remarks>
    public static Result<Inventory> Create(InventoryId id, int capacity)
    {
        if (capacity <= 0)
            return Result.Failure<Inventory>("Capacity must be positive");

        if (capacity > 100)
            return Result.Failure<Inventory>("Capacity cannot exceed 100");

        // Map capacity to grid dimensions (square root approximation)
        int gridWidth = (int)Math.Ceiling(Math.Sqrt(capacity));
        int gridHeight = (int)Math.Ceiling((double)capacity / gridWidth);

        return Create(id, gridWidth, gridHeight, ContainerType.General);
    }

    /// <summary>
    /// Creates a new inventory with explicit grid dimensions (VS_018 signature).
    /// </summary>
    /// <param name="id">Unique inventory identifier</param>
    /// <param name="gridWidth">Grid width in cells (must be positive)</param>
    /// <param name="gridHeight">Grid height in cells (must be positive)</param>
    /// <param name="containerType">Container type (default: General)</param>
    /// <returns>Result containing new Inventory or error message</returns>
    public static Result<Inventory> Create(
        InventoryId id,
        int gridWidth,
        int gridHeight,
        ContainerType containerType = ContainerType.General)
    {
        if (gridWidth <= 0)
            return Result.Failure<Inventory>("Grid width must be positive");

        if (gridHeight <= 0)
            return Result.Failure<Inventory>("Grid height must be positive");

        int capacity = gridWidth * gridHeight;
        if (capacity > 100)
            return Result.Failure<Inventory>("Grid capacity cannot exceed 100");

        return Result.Success(new Inventory(id, gridWidth, gridHeight, containerType));
    }

    /// <summary>
    /// Places an item at a specific grid position.
    /// </summary>
    /// <param name="itemId">ID of item to place</param>
    /// <param name="position">Grid position to place at</param>
    /// <returns>Success if placed, Failure with reason if not</returns>
    public Result PlaceItemAt(ItemId itemId, GridPosition position)
    {
        // BUSINESS RULE: Position must be within grid bounds
        if (position.X < 0 || position.X >= GridWidth ||
            position.Y < 0 || position.Y >= GridHeight)
            return Result.Failure("Position is out of bounds");

        // BUSINESS RULE: Position must be free
        if (_itemPositions.Values.Contains(position))
            return Result.Failure("Position is occupied");

        // BUSINESS RULE: Cannot add duplicate items
        if (_itemPositions.ContainsKey(itemId))
            return Result.Failure("Item already in inventory");

        _itemPositions[itemId] = position;
        return Result.Success();
    }

    /// <summary>
    /// Checks if a position is free (within bounds and unoccupied).
    /// </summary>
    public bool CanPlaceAt(GridPosition position)
    {
        if (position.X < 0 || position.X >= GridWidth ||
            position.Y < 0 || position.Y >= GridHeight)
            return false;

        return !_itemPositions.Values.Contains(position);
    }

    /// <summary>
    /// Gets the grid position of an item.
    /// </summary>
    /// <param name="itemId">ID of item to locate</param>
    /// <returns>Grid position of the item</returns>
    /// <exception cref="InvalidOperationException">If item not in inventory</exception>
    public GridPosition GetItemPosition(ItemId itemId)
    {
        if (!_itemPositions.TryGetValue(itemId, out var position))
            throw new InvalidOperationException("Item not found in inventory");

        return position;
    }

    /// <summary>
    /// Gets the item at a specific grid position.
    /// </summary>
    /// <param name="position">Grid position to query</param>
    /// <returns>Result containing ItemId if occupied, Failure if empty</returns>
    public Result<ItemId> GetItemAtPosition(GridPosition position)
    {
        var item = _itemPositions.FirstOrDefault(kvp => kvp.Value == position);
        if (item.Key == default)
            return Result.Failure<ItemId>("No item at position");

        return Result.Success(item.Key);
    }

    /// <summary>
    /// Adds an item to this inventory (auto-placement for General containers).
    /// </summary>
    /// <param name="itemId">ID of item to add</param>
    /// <returns>Success if added, Failure with reason if not</returns>
    /// <remarks>
    /// BACKWARD COMPATIBILITY: VS_008 code uses this method.
    /// Auto-places at first free position (top-left to bottom-right scan).
    /// Only supported for ContainerType.General (spatial containers should use PlaceItemAt).
    /// </remarks>
    public Result AddItem(ItemId itemId)
    {
        // BUSINESS RULE: Cannot add to full inventory
        if (IsFull)
            return Result.Failure("Inventory is full");

        // BUSINESS RULE: Cannot add duplicate items
        if (_itemPositions.ContainsKey(itemId))
            return Result.Failure("Item already in inventory");

        // Find first free position (top-left to bottom-right)
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                var position = new GridPosition(x, y);
                if (CanPlaceAt(position))
                {
                    _itemPositions[itemId] = position;
                    return Result.Success();
                }
            }
        }

        return Result.Failure("Inventory is full");
    }

    /// <summary>
    /// Removes an item from this inventory.
    /// </summary>
    /// <param name="itemId">ID of item to remove</param>
    /// <returns>Success if removed, Failure if not found</returns>
    public Result RemoveItem(ItemId itemId)
    {
        if (!_itemPositions.ContainsKey(itemId))
            return Result.Failure("Item not found in inventory");

        _itemPositions.Remove(itemId);
        return Result.Success();
    }

    /// <summary>
    /// Checks if this inventory contains an item.
    /// </summary>
    public bool Contains(ItemId itemId) => _itemPositions.ContainsKey(itemId);

    /// <summary>
    /// Removes all items from this inventory.
    /// </summary>
    public void Clear() => _itemPositions.Clear();
}
