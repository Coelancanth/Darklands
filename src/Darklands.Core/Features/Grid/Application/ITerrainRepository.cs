using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Domain;

namespace Darklands.Core.Features.Grid.Application;

/// <summary>
/// Repository contract for terrain catalog management.
/// Defined in Application layer (Dependency Inversion Principle).
/// Implemented in Infrastructure layer.
/// </summary>
/// <remarks>
/// CATALOG vs RUNTIME DATA:
/// - Terrains are catalog data (loaded once at startup from TileSet)
/// - Unlike GridMap (runtime state), TerrainDefinitions are read-only reference data
/// - No Save/Delete methods - TileSet is single source of truth
///
/// SYNCHRONOUS DESIGN:
/// - Methods are synchronous (catalog loaded at startup, cached in memory)
/// - No need for async - TileSet resource loading happens once
///
/// PHASE 1 vs PHASE 2:
/// - Phase 1: StubTerrainRepository (hardcoded catalog matching old TerrainType enum)
/// - Phase 2: TileSetTerrainRepository (reads TileSet custom data)
/// </remarks>
public interface ITerrainRepository
{
    /// <summary>
    /// Retrieves a terrain definition by name.
    /// </summary>
    /// <param name="name">Terrain name (e.g., "floor", "wall", "smoke", "tree")</param>
    /// <returns>Result containing TerrainDefinition or error if not found</returns>
    /// <remarks>
    /// Name matching is case-insensitive.
    /// Used by SetTerrainCommand to resolve names to definitions.
    /// </remarks>
    Result<TerrainDefinition> GetByName(string name);

    /// <summary>
    /// Retrieves all terrain definitions in the catalog.
    /// Used for terrain palette UI, level editor, debugging.
    /// </summary>
    /// <returns>Result containing list of all terrains (empty list if catalog not loaded)</returns>
    Result<List<TerrainDefinition>> GetAll();

    /// <summary>
    /// Retrieves the default terrain used for grid initialization.
    /// </summary>
    /// <returns>Result containing default TerrainDefinition (typically "floor")</returns>
    /// <remarks>
    /// Used by GridMap constructor to initialize all cells.
    /// Equivalent to old TerrainType.Floor default.
    /// </remarks>
    Result<TerrainDefinition> GetDefault();
}
