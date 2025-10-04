using CSharpFunctionalExtensions;

namespace Darklands.Core.Features.Grid.Domain;

/// <summary>
/// Unique identifier for a terrain type in the terrain catalog.
/// Uses simple integer mapping (0=Floor, 1=Wall, 2=Smoke, etc.).
/// </summary>
/// <remarks>
/// DESIGN DECISION: int over GUID
/// - Terrain catalog is small (~10-20 types maximum)
/// - Designer-friendly sequential IDs (easier to reference in TileSet)
/// - Direct mapping to TileSet custom_data_0 (terrain_id)
/// - Performance: int comparison faster than GUID
/// </remarks>
public readonly record struct TerrainId
{
    public int Value { get; init; }

    private TerrainId(int value)
    {
        Value = value;
    }

    public static Result<TerrainId> Create(int value)
    {
        if (value < 0)
        {
            return Result.Failure<TerrainId>("TerrainId must be non-negative");
        }

        return Result.Success(new TerrainId(value));
    }

    public static TerrainId Unsafe(int value) => new(value);

    public override string ToString() => Value.ToString();

    // Convenience constants for common terrain types
    public static TerrainId Floor => new(0);
    public static TerrainId Wall => new(1);
    public static TerrainId Smoke => new(2);
}
