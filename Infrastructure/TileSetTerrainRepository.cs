using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Application;
using Darklands.Core.Features.Grid.Domain;
using Godot;
using Microsoft.Extensions.Logging;

namespace Darklands.Infrastructure;

/// <summary>
/// Terrain repository that auto-discovers terrains from Godot TileSet resource.
/// Follows VS_009 TileSetItemRepository pattern for catalog data.
/// </summary>
/// <remarks>
/// ARCHITECTURE (ADR-002 Compliance):
/// - Lives in Presentation layer (Infrastructure folder) because it uses Godot types
/// - Extracts primitives from TileSet custom data
/// - Calls TerrainDefinition.Create() with primitives (Domain stays Godot-free)
///
/// AUTO-DISCOVERY WORKFLOW:
/// 1. Constructor receives TileSet loaded by DI container setup
/// 2. Enumerates all tiles in atlas source 4 (terrain atlas)
/// 3. Reads custom data: name (layer 0), can_pass (layer 1), can_see_through (layer 2)
/// 4. Calls TerrainDefinition.Create() with primitives
/// 5. Caches in dictionary for O(1) queries
///
/// TILESET CUSTOM DATA LAYERS:
/// - Layer 0 (name): String - Terrain identifier (e.g., "floor", "wall", "smoke", "tree")
/// - Layer 1 (can_pass): Bool - Can actors walk through this terrain?
/// - Layer 2 (can_see_through): Bool - Is this terrain transparent for FOV?
///
/// PERFORMANCE:
/// - Loading happens once at DI container setup (GameStrapper)
/// - All queries are O(1) dictionary lookups or cached list returns
/// - No runtime I/O or Godot resource loading during queries
/// </remarks>
public sealed class TileSetTerrainRepository : ITerrainRepository
{
    private readonly Dictionary<string, TerrainDefinition> _terrainsByName = new();
    private readonly List<TerrainDefinition> _allTerrains = new();
    private readonly TerrainDefinition _defaultTerrain;
    private readonly ILogger<TileSetTerrainRepository> _logger;

    public TileSetTerrainRepository(
        TileSet terrainTileSet,
        ILogger<TileSetTerrainRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (terrainTileSet == null)
        {
            throw new ArgumentNullException(nameof(terrainTileSet), "Terrain TileSet cannot be null");
        }

        LoadTerrainsFromTileSet(terrainTileSet);

        // Set default terrain (floor)
        if (_terrainsByName.TryGetValue("floor", out var floor))
        {
            _defaultTerrain = floor;
        }
        else
        {
            throw new InvalidOperationException(
                "TileSet must contain a terrain named 'floor' for grid initialization");
        }
    }

    private void LoadTerrainsFromTileSet(TileSet tileSet)
    {
        _logger.LogInformation("Auto-discovering terrains from TileSet...");

        // Get atlas source 4 (terrain atlas in test_terrain_tileset.tres)
        var sourceId = 4;
        var atlasSource = tileSet.GetSource(sourceId) as TileSetAtlasSource;

        if (atlasSource == null)
        {
            _logger.LogWarning("TileSet has no atlas source at index {SourceId}", sourceId);
            return;
        }

        var tilesCount = atlasSource.GetTilesCount();
        _logger.LogDebug("Found {TilesCount} tiles in TileSet atlas source", tilesCount);

        for (int i = 0; i < tilesCount; i++)
        {
            var tileId = atlasSource.GetTileId(i);
            var result = LoadTerrainFromTile(atlasSource, tileId);

            if (result.IsSuccess)
            {
                var terrain = result.Value;
                _terrainsByName[terrain.Name] = terrain;
                _allTerrains.Add(terrain);

                _logger.LogDebug(
                    "Loaded terrain: {Name} (atlas: {X},{Y}, passable: {CanPass}, transparent: {CanSeeThrough})",
                    terrain.Name,
                    terrain.AtlasX,
                    terrain.AtlasY,
                    terrain.CanPass,
                    terrain.CanSeeThrough);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to load terrain at atlas coords {X},{Y}: {Error}",
                    tileId.X,
                    tileId.Y,
                    result.Error);
            }
        }

        _logger.LogInformation(
            "Terrain catalog loaded: {TerrainCount} terrains discovered",
            _allTerrains.Count);
    }

    private Result<TerrainDefinition> LoadTerrainFromTile(TileSetAtlasSource atlasSource, Vector2I tileCoords)
    {
        // Get tile data (contains custom data layers)
        var tileData = atlasSource.GetTileData(tileCoords, alternativeTile: 0);

        if (tileData == null)
        {
            return Result.Failure<TerrainDefinition>("TileData is null");
        }

        // Read custom data layers
        var nameVariant = tileData.GetCustomData("name");
        var canPassVariant = tileData.GetCustomData("can_pass");
        var canSeeThroughVariant = tileData.GetCustomData("can_see_through");

        // Validate name (required)
        if (nameVariant.VariantType != Variant.Type.String)
        {
            return Result.Failure<TerrainDefinition>(
                $"Tile at ({tileCoords.X},{tileCoords.Y}) missing 'name' custom data");
        }

        var name = nameVariant.AsString();

        // FIX: Remove surrounding quotes if present (Godot metadata quirk)
        // AsString() sometimes returns "\"wall\"" instead of "wall"
        name = name.Trim('"');

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<TerrainDefinition>(
                $"Tile at ({tileCoords.X},{tileCoords.Y}) has empty 'name' custom data");
        }

        // Parse can_pass (default: true if missing)
        bool canPass = canPassVariant.VariantType == Variant.Type.Bool
            ? canPassVariant.AsBool()
            : true;

        // Parse can_see_through (default: true if missing)
        bool canSeeThrough = canSeeThroughVariant.VariantType == Variant.Type.Bool
            ? canSeeThroughVariant.AsBool()
            : true;

        // Create TerrainDefinition (Atlas coords as primitives for ADR-002 compliance)
        return TerrainDefinition.Create(
            name: name,
            canPass: canPass,
            canSeeThrough: canSeeThrough,
            atlasX: tileCoords.X,
            atlasY: tileCoords.Y);
    }

    public Result<TerrainDefinition> GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<TerrainDefinition>("Terrain name cannot be empty");
        }

        var normalizedName = name.ToLowerInvariant();

        if (_terrainsByName.TryGetValue(normalizedName, out var terrain))
        {
            return Result.Success(terrain);
        }

        return Result.Failure<TerrainDefinition>($"Terrain '{name}' not found in catalog");
    }

    public Result<List<TerrainDefinition>> GetAll()
    {
        return Result.Success(_allTerrains);
    }

    public Result<TerrainDefinition> GetDefault()
    {
        return Result.Success(_defaultTerrain);
    }
}
