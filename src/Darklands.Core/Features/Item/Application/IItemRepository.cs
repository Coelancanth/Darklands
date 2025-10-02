using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using ItemEntity = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Features.Item.Application;

/// <summary>
/// Repository contract for item catalog management.
/// Defined in Application layer (Dependency Inversion Principle).
/// Implemented in Infrastructure layer.
/// </summary>
/// <remarks>
/// CATALOG vs RUNTIME DATA:
/// - Items are catalog data (loaded once at startup from TileSet)
/// - Unlike Inventory (runtime state), Items are read-only reference data
/// - No Save/Delete methods - TileSet is single source of truth
///
/// SYNCHRONOUS DESIGN:
/// - Methods are synchronous (catalog loaded at startup, cached in memory)
/// - No need for async - TileSet resource loading happens once
/// </remarks>
public interface IItemRepository
{
    /// <summary>
    /// Retrieves an item by its unique ID.
    /// </summary>
    /// <param name="itemId">Unique item identifier</param>
    /// <returns>Result containing Item or error if not found</returns>
    Result<ItemEntity> GetById(ItemId itemId);

    /// <summary>
    /// Retrieves an item by its unique ID (async wrapper for handler compatibility).
    /// </summary>
    /// <param name="itemId">Unique item identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing Item or error if not found</returns>
    Task<Result<ItemEntity>> GetByIdAsync(ItemId itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all items in the catalog.
    /// Used for item showcase UI, inventory dropdowns, loot tables.
    /// </summary>
    /// <returns>Result containing list of all items (empty list if catalog not loaded)</returns>
    Result<List<ItemEntity>> GetAll();

    /// <summary>
    /// Retrieves all items of a specific type.
    /// Used for filtering (e.g., "show all weapons", "show all consumables").
    /// </summary>
    /// <param name="type">Item type from TileSet custom_data_1 (e.g., "weapon", "item", "UI")</param>
    /// <returns>Result containing list of matching items (empty list if no matches)</returns>
    Result<List<ItemEntity>> GetByType(string type);
}
