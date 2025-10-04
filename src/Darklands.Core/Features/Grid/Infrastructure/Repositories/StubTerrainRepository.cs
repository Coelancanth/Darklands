using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Application;
using Darklands.Core.Features.Grid.Domain;

namespace Darklands.Core.Features.Grid.Infrastructure.Repositories;

/// <summary>
/// Temporary hardcoded terrain catalog for Phase 1.
/// Provides same terrains as legacy TerrainType enum (Floor, Wall, Smoke).
/// Will be replaced by TileSetTerrainRepository in Phase 2.
/// </summary>
/// <remarks>
/// PHASE 1 STRATEGY:
/// - Hardcoded catalog matches old TerrainType enum exactly
/// - Enables deleting TerrainType.cs without breaking tests
/// - Atlas coordinates are placeholders (updated in Phase 2 from TileSet)
///
/// PHASE 2 REPLACEMENT:
/// - TileSetTerrainRepository reads TileSet custom data
/// - Auto-discovers terrains from atlas (floor, wall variants, smoke, tree)
/// - No code changes needed in GridMap or commands
/// </remarks>
public sealed class StubTerrainRepository : ITerrainRepository
{
    private readonly Dictionary<string, TerrainDefinition> _terrains;
    private readonly TerrainDefinition _defaultTerrain;

    public StubTerrainRepository()
    {
        // Initialize hardcoded catalog matching old TerrainType enum
        _terrains = new Dictionary<string, TerrainDefinition>
        {
            ["floor"] = TerrainDefinition.Create(
                name: "floor",
                canPass: true,
                canSeeThrough: true,
                atlasX: 1,  // Placeholder (updated from TileSet in Phase 2)
                atlasY: 1
            ).Value,

            ["wall"] = TerrainDefinition.Create(
                name: "wall",
                canPass: false,
                canSeeThrough: false,
                atlasX: 0,  // Placeholder
                atlasY: 0
            ).Value,

            ["smoke"] = TerrainDefinition.Create(
                name: "smoke",
                canPass: true,        // Passable
                canSeeThrough: false, // Opaque (blocks vision)
                atlasX: 15, // Placeholder
                atlasY: 3
            ).Value,
        };

        _defaultTerrain = _terrains["floor"];
    }

    public Result<TerrainDefinition> GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<TerrainDefinition>("Terrain name cannot be empty");
        }

        var normalizedName = name.ToLowerInvariant();

        if (_terrains.TryGetValue(normalizedName, out var terrain))
        {
            return Result.Success(terrain);
        }

        return Result.Failure<TerrainDefinition>($"Terrain '{name}' not found in catalog");
    }

    public Result<List<TerrainDefinition>> GetAll()
    {
        return Result.Success(_terrains.Values.ToList());
    }

    public Result<TerrainDefinition> GetDefault()
    {
        return Result.Success(_defaultTerrain);
    }
}
