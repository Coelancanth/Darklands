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
/// - VS_018 Phase 1: Spatial grid with positions (drag-drop UX, 1×1 items only)
/// - VS_018 Phase 2: Multi-cell items (Width×Height footprints, rectangle collision)
/// - Dictionary primary storage ensures single source of truth
/// - Items property computed for backward compatibility
/// </remarks>
public sealed class Inventory
{
    private readonly Dictionary<ItemId, GridPosition> _itemPositions;
    private readonly Dictionary<ItemId, (int width, int height)> _itemDimensions; // Phase 2: Cache dimensions for collision
    private readonly Dictionary<ItemId, Rotation> _itemRotations; // Phase 3: Rotation state per placement

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

    /// <summary>
    /// Read-only view of item dimensions (ItemId → (Width, Height) mapping).
    /// Phase 2: Needed for rendering and collision detection.
    /// </summary>
    public IReadOnlyDictionary<ItemId, (int width, int height)> ItemDimensions => _itemDimensions;

    /// <summary>
    /// Read-only view of item rotations (ItemId → Rotation mapping).
    /// Phase 3: Needed for rendering rotated sprites and calculating effective dimensions.
    /// </summary>
    public IReadOnlyDictionary<ItemId, Rotation> ItemRotations => _itemRotations;

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
        _itemDimensions = new Dictionary<ItemId, (int, int)>(Capacity); // Phase 2
        _itemRotations = new Dictionary<ItemId, Rotation>(Capacity); // Phase 3
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
    /// Places an item at a specific grid position (Phase 1: 1×1 items only).
    /// </summary>
    /// <param name="itemId">ID of item to place</param>
    /// <param name="position">Grid position to place at</param>
    /// <returns>Success if placed, Failure with reason if not</returns>
    /// <remarks>
    /// BACKWARD COMPATIBILITY: This overload assumes 1×1 dimensions and 0° rotation.
    /// Phase 2 code should use PlaceItemAt(itemId, position, width, height).
    /// Phase 3 code should use PlaceItemAt(itemId, position, width, height, rotation).
    /// </remarks>
    public Result PlaceItemAt(ItemId itemId, GridPosition position)
    {
        return PlaceItemAt(itemId, position, width: 1, height: 1, rotation: Rotation.Degrees0);
    }

    /// <summary>
    /// Places an item at a specific grid position with dimensions (Phase 2: multi-cell items).
    /// </summary>
    /// <param name="itemId">ID of item to place</param>
    /// <param name="position">Top-left grid position (origin)</param>
    /// <param name="width">Item width in cells (base, unrotated)</param>
    /// <param name="height">Item height in cells (base, unrotated)</param>
    /// <returns>Success if placed, Failure with reason if not</returns>
    /// <remarks>
    /// BACKWARD COMPATIBILITY: Phase 2 overload assumes 0° rotation.
    /// Phase 3 code should use PlaceItemAt(itemId, position, width, height, rotation).
    /// </remarks>
    public Result PlaceItemAt(ItemId itemId, GridPosition position, int width, int height)
    {
        return PlaceItemAt(itemId, position, width, height, rotation: Rotation.Degrees0);
    }

    /// <summary>
    /// Places an item at a specific grid position with dimensions and rotation (Phase 3: rotatable items).
    /// BACKWARD COMPATIBILITY: Converts width×height to rectangle shape internally.
    /// </summary>
    /// <param name="itemId">ID of item to place</param>
    /// <param name="position">Top-left grid position (origin)</param>
    /// <param name="width">Item width in cells (base, unrotated)</param>
    /// <param name="height">Item height in cells (base, unrotated)</param>
    /// <param name="rotation">Rotation state (affects effective dimensions)</param>
    /// <returns>Success if placed, Failure with reason if not</returns>
    public Result PlaceItemAt(ItemId itemId, GridPosition position, int width, int height, Rotation rotation)
    {
        // BACKWARD COMPATIBILITY: Convert dimensions to rectangle shape
        var shapeResult = ItemShape.CreateRectangle(width, height);
        if (shapeResult.IsFailure)
            return Result.Failure(shapeResult.Error);

        // Apply rotation to shape
        var shape = shapeResult.Value;
        for (int i = 0; i < ((int)rotation / 90); i++)
        {
            var rotateResult = shape.RotateClockwise();
            if (rotateResult.IsFailure)
                return Result.Failure(rotateResult.Error);
            shape = rotateResult.Value;
        }

        // Delegate to shape-based placement
        return PlaceItemWithShape(itemId, position, width, height, shape, rotation);
    }

    /// <summary>
    /// Places an item using explicit ItemShape with rotation (Phase 4 PUBLIC API).
    /// Enables L/T/Z-shapes by using item's actual OccupiedCells.
    /// </summary>
    /// <param name="itemId">ID of item to place</param>
    /// <param name="position">Top-left grid position (anchor)</param>
    /// <param name="shape">ItemShape from Item.Shape (base, unrotated)</param>
    /// <param name="rotation">Rotation to apply</param>
    /// <returns>Success if placed, Failure with reason if not</returns>
    public Result PlaceItemAt(ItemId itemId, GridPosition position, ItemShape shape, Rotation rotation)
    {
        // Apply rotation to shape
        var rotatedShape = shape;
        for (int i = 0; i < ((int)rotation / 90); i++)
        {
            var rotateResult = rotatedShape.RotateClockwise();
            if (rotateResult.IsFailure)
                return Result.Failure(rotateResult.Error);
            rotatedShape = rotateResult.Value;
        }

        // Delegate to core placement logic
        // Store base (unrotated) dimensions for backward compat
        return PlaceItemWithShape(itemId, position, shape.Width, shape.Height, rotatedShape, rotation);
    }

    /// <summary>
    /// Places an item using explicit ItemShape (Phase 4: CORE COLLISION LOGIC).
    /// Uses OccupiedCells iteration for L/T-shape compatibility.
    /// </summary>
    /// <param name="itemId">ID of item to place</param>
    /// <param name="position">Top-left grid position (anchor)</param>
    /// <param name="baseWidth">Base width for backward compat storage</param>
    /// <param name="baseHeight">Base height for backward compat storage</param>
    /// <param name="shape">ItemShape with OccupiedCells (after rotation applied)</param>
    /// <param name="rotation">Rotation state for storage</param>
    /// <returns>Success if placed, Failure with reason if not</returns>
    private Result PlaceItemWithShape(
        ItemId itemId,
        GridPosition position,
        int baseWidth,
        int baseHeight,
        ItemShape shape,
        Rotation rotation)
    {
        // BUSINESS RULE: Cannot add duplicate items
        if (_itemPositions.ContainsKey(itemId))
            return Result.Failure("Item already in inventory");

        // PHASE 4: Check bounds for ALL occupied cells (not just bounding box)
        foreach (var offset in shape.OccupiedCells)
        {
            var targetPos = new GridPosition(position.X + offset.X, position.Y + offset.Y);

            if (targetPos.X < 0 || targetPos.X >= GridWidth ||
                targetPos.Y < 0 || targetPos.Y >= GridHeight)
            {
                return Result.Failure("Item footprint exceeds grid bounds");
            }
        }

        // PHASE 4: Check collision with existing items (cell-by-cell, NOT AABB)
        // For each occupied cell of the new item, check if ANY existing item occupies that cell
        var occupiedCells = new HashSet<GridPosition>();

        foreach (var (existingItemId, existingOrigin) in _itemPositions)
        {
            var (existingBaseWidth, existingBaseHeight) = _itemDimensions.GetValueOrDefault(existingItemId, (1, 1));
            var existingRotation = _itemRotations.GetValueOrDefault(existingItemId, Rotation.Degrees0);

            // Reconstruct existing item's shape
            var existingShapeResult = ItemShape.CreateRectangle(existingBaseWidth, existingBaseHeight);
            if (existingShapeResult.IsFailure)
                continue; // Skip malformed items (shouldn't happen)

            var existingShape = existingShapeResult.Value;

            // Apply existing item's rotation
            for (int i = 0; i < ((int)existingRotation / 90); i++)
            {
                var rotResult = existingShape.RotateClockwise();
                if (rotResult.IsSuccess)
                    existingShape = rotResult.Value;
            }

            // Add all cells occupied by this existing item
            foreach (var offset in existingShape.OccupiedCells)
            {
                occupiedCells.Add(new GridPosition(
                    existingOrigin.X + offset.X,
                    existingOrigin.Y + offset.Y));
            }
        }

        // Check if ANY of the new item's cells overlap with occupied cells
        foreach (var offset in shape.OccupiedCells)
        {
            var targetPos = new GridPosition(position.X + offset.X, position.Y + offset.Y);

            if (occupiedCells.Contains(targetPos))
            {
                return Result.Failure($"Item footprint overlaps with existing item at cell ({targetPos.X}, {targetPos.Y})");
            }
        }

        // All validations passed - place item
        _itemPositions[itemId] = position;
        _itemDimensions[itemId] = (baseWidth, baseHeight); // Store BASE dimensions (backward compat)
        _itemRotations[itemId] = rotation;
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
    /// <returns>Success with grid position, or Failure if item not in inventory</returns>
    public Result<GridPosition> GetItemPosition(ItemId itemId)
    {
        if (!_itemPositions.TryGetValue(itemId, out var position))
            return Result.Failure<GridPosition>("Item not found in inventory");

        return Result.Success(position);
    }

    /// <summary>
    /// Gets the rotation state of an item.
    /// </summary>
    /// <param name="itemId">ID of item to query</param>
    /// <returns>Success with rotation state, or Failure if item not in inventory</returns>
    /// <remarks>
    /// PHASE 3: Returns Degrees0 for items placed before Phase 3 (backward compatibility).
    /// </remarks>
    public Result<Rotation> GetItemRotation(ItemId itemId)
    {
        if (!_itemPositions.ContainsKey(itemId))
            return Result.Failure<Rotation>("Item not found in inventory");

        // Return stored rotation, or Degrees0 if not set (backward compat)
        var rotation = _itemRotations.GetValueOrDefault(itemId, Rotation.Degrees0);
        return Result.Success(rotation);
    }

    /// <summary>
    /// Rotates an item in place (Phase 3).
    /// </summary>
    /// <param name="itemId">ID of item to rotate</param>
    /// <param name="newRotation">New rotation state</param>
    /// <returns>Success if rotated, Failure if new orientation doesn't fit</returns>
    /// <remarks>
    /// VALIDATION: Ensures rotated item still fits within grid bounds and doesn't collide.
    /// EXAMPLE: 2×1 sword at edge rotating to 1×2 may exceed bounds → Failure.
    /// </remarks>
    public Result RotateItem(ItemId itemId, Rotation newRotation)
    {
        // BUSINESS RULE: Item must exist in inventory
        if (!_itemPositions.TryGetValue(itemId, out var position))
            return Result.Failure("Item not found in inventory");

        var (baseWidth, baseHeight) = _itemDimensions.GetValueOrDefault(itemId, (1, 1));

        // Calculate new effective dimensions after rotation
        var (newEffectiveWidth, newEffectiveHeight) =
            RotationHelper.GetRotatedDimensions(baseWidth, baseHeight, newRotation);

        // BUSINESS RULE: Rotated item must fit within grid bounds
        if (position.X + newEffectiveWidth > GridWidth ||
            position.Y + newEffectiveHeight > GridHeight)
            return Result.Failure("Rotated item would exceed grid bounds");

        // BUSINESS RULE: Rotated item must not overlap with other items
        foreach (var (existingItemId, existingOrigin) in _itemPositions)
        {
            // Skip self-collision check
            if (existingItemId == itemId)
                continue;

            var (existingBaseWidth, existingBaseHeight) = _itemDimensions.GetValueOrDefault(existingItemId, (1, 1));
            var existingRotation = _itemRotations.GetValueOrDefault(existingItemId, Rotation.Degrees0);

            var (existingEffectiveWidth, existingEffectiveHeight) =
                RotationHelper.GetRotatedDimensions(existingBaseWidth, existingBaseHeight, existingRotation);

            // Rectangle overlap test (AABB collision)
            bool overlaps = !(position.X >= existingOrigin.X + existingEffectiveWidth ||
                              position.X + newEffectiveWidth <= existingOrigin.X ||
                              position.Y >= existingOrigin.Y + existingEffectiveHeight ||
                              position.Y + newEffectiveHeight <= existingOrigin.Y);

            if (overlaps)
                return Result.Failure($"Rotated item would overlap with item at ({existingOrigin.X}, {existingOrigin.Y})");
        }

        // All validations passed - update rotation
        _itemRotations[itemId] = newRotation;
        return Result.Success();
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
                    // Use PlaceItemAt for consistency (assumes 1×1 for backward compat)
                    _itemPositions[itemId] = position;
                    _itemDimensions[itemId] = (1, 1); // Phase 2: Default to 1×1
                    _itemRotations[itemId] = Rotation.Degrees0; // Phase 3: Default rotation
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
        _itemDimensions.Remove(itemId); // Phase 2: Clean up dimensions
        _itemRotations.Remove(itemId);  // Phase 3: Clean up rotation
        return Result.Success();
    }

    /// <summary>
    /// Checks if this inventory contains an item.
    /// </summary>
    public bool Contains(ItemId itemId) => _itemPositions.ContainsKey(itemId);

    /// <summary>
    /// Removes all items from this inventory.
    /// </summary>
    public void Clear()
    {
        _itemPositions.Clear();
        _itemDimensions.Clear(); // Phase 2: Clear dimensions
        _itemRotations.Clear();  // Phase 3: Clear rotations
    }
}
