using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Handler for GetOccupiedCellsQuery.
/// Returns absolute grid positions occupied by an item (SSOT for occupied cell calculation).
/// </summary>
/// <remarks>
/// TD_004 Leak #2: Eliminates Presentation's shape rotation and cell iteration logic.
///
/// ALGORITHM:
/// 1. Get inventory and find item's position + rotation
/// 2. Core already knows occupied cells (stored when item was placed)
/// 3. Return absolute positions directly - Presentation just renders
///
/// REPLACES: 43 lines of Presentation logic (rotation loops, cell iteration, fallback math)
/// </remarks>
public sealed class GetOccupiedCellsQueryHandler
    : IRequestHandler<GetOccupiedCellsQuery, Result<List<GridPosition>>>
{
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<GetOccupiedCellsQueryHandler> _logger;

    public GetOccupiedCellsQueryHandler(
        IInventoryRepository inventories,
        ILogger<GetOccupiedCellsQueryHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<GridPosition>>> Handle(
        GetOccupiedCellsQuery query,
        CancellationToken cancellationToken)
    {
        // Get inventory
        var inventoryResult = await _inventories.GetByActorIdAsync(query.ContainerId, cancellationToken);
        if (inventoryResult.IsFailure)
            return Result.Failure<List<GridPosition>>(inventoryResult.Error);

        var inventory = inventoryResult.Value;

        // Get item's position (anchor point)
        var positionResult = inventory.GetItemPosition(query.ItemId);
        if (positionResult.IsFailure)
            return Result.Failure<List<GridPosition>>($"Item {query.ItemId} not found in inventory");

        var origin = positionResult.Value;

        // Get item's BASE shape (unrotated)
        // WHY: Inventory stores base shape, applies rotation separately
        if (!inventory.ItemShapes.TryGetValue(query.ItemId, out var baseShape))
            return Result.Failure<List<GridPosition>>($"Item {query.ItemId} has no shape data");

        // Get item's rotation
        var rotationResult = inventory.GetItemRotation(query.ItemId);
        if (rotationResult.IsFailure)
            return Result.Failure<List<GridPosition>>(rotationResult.Error);

        var rotation = rotationResult.Value;

        // Apply rotation to shape
        var rotatedShape = ApplyRotation(baseShape, rotation);

        // Convert relative offsets to absolute positions
        // WHY: Shape.OccupiedCells are relative (0,0), (0,1), etc. - add origin to get world positions
        var absoluteCells = rotatedShape.OccupiedCells
            .Select(offset => new GridPosition(origin.X + offset.X, origin.Y + offset.Y))
            .ToList();

        _logger.LogDebug("Item {ItemId} at ({X},{Y}) occupies {CellCount} cells in container {ContainerId}",
            query.ItemId, origin.X, origin.Y, absoluteCells.Count, query.ContainerId);

        return Result.Success(absoluteCells);
    }

    /// <summary>
    /// Applies rotation to shape by rotating clockwise N times (90° increments).
    /// </summary>
    private ItemShape ApplyRotation(ItemShape baseShape, Rotation rotation)
    {
        var rotated = baseShape;
        int rotations = (int)rotation / 90; // Degrees0=0, Degrees90=1, Degrees180=2, Degrees270=3

        for (int i = 0; i < rotations; i++)
        {
            var rotateResult = rotated.RotateClockwise();
            if (rotateResult.IsSuccess)
            {
                rotated = rotateResult.Value;
            }
            else
            {
                // Should never fail for valid shapes, but log if it does
                _logger.LogWarning("Rotation failed: {Error}", rotateResult.Error);
                break;
            }
        }

        return rotated;
    }
}
