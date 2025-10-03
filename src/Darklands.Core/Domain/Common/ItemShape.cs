using CSharpFunctionalExtensions;

namespace Darklands.Core.Domain.Common;

/// <summary>
/// Represents the spatial shape of an item in inventory grid space.
/// Defines which cells an item occupies (via coordinate list) + bounding box dimensions.
/// </summary>
/// <remarks>
/// ARCHITECTURE (VS_018 Phase 4): OccupiedCells is SINGLE SOURCE OF TRUTH for collision.
/// - Rectangle (2×3): All 6 cells occupied → Width=2, Height=3, OccupiedCells.Count=6
/// - L-shape (2×2 bounding box): Only 3 cells occupied → Width=2, Height=2, OccupiedCells.Count=3
///
/// WHY TWO REPRESENTATIONS:
/// - OccupiedCells: Used for collision detection (iterate actual occupied cells)
/// - Width/Height: Bounding box metadata (quick bounds checks, rotation dimension swap)
///
/// ROTATION BEHAVIOR:
/// - RotateClockwise() transforms OccupiedCells coordinates AND swaps Width↔Height
/// - Rectangle (2×3) → Rectangle (3×2) after rotation
/// - L-shape (2×2) → L-shape (2×2) after rotation (square, dimensions unchanged)
///
/// ENCODING FORMATS:
/// - "rect:WxH" (optimized): Rectangle where all W×H cells are occupied
/// - "custom:x1,y1;x2,y2;..." (general): Explicit list of occupied cell coordinates
/// </remarks>
public sealed class ItemShape
{
    /// <summary>
    /// Bounding box width (horizontal extent of shape).
    /// </summary>
    public int Width { get; private init; }

    /// <summary>
    /// Bounding box height (vertical extent of shape).
    /// </summary>
    public int Height { get; private init; }

    /// <summary>
    /// List of occupied cells relative to anchor point (0,0).
    /// SINGLE SOURCE OF TRUTH for collision detection.
    /// </summary>
    public IReadOnlyList<GridPosition> OccupiedCells { get; private init; }

    private ItemShape(int width, int height, IReadOnlyList<GridPosition> occupiedCells)
    {
        Width = width;
        Height = height;
        OccupiedCells = occupiedCells;
    }

    /// <summary>
    /// Creates a rectangular shape where ALL Width×Height cells are occupied.
    /// </summary>
    /// <param name="width">Bounding box width (must be positive)</param>
    /// <param name="height">Bounding box height (must be positive)</param>
    /// <returns>Result containing ItemShape or validation error</returns>
    public static Result<ItemShape> CreateRectangle(int width, int height)
    {
        // BUSINESS RULE: Dimensions must be positive
        if (width <= 0)
            return Result.Failure<ItemShape>("Width must be positive");

        if (height <= 0)
            return Result.Failure<ItemShape>("Height must be positive");

        // Generate all W×H cells
        var cells = new List<GridPosition>(width * height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells.Add(new GridPosition(x, y));
            }
        }

        return Result.Success(new ItemShape(width, height, cells));
    }

    /// <summary>
    /// Creates a shape from encoding string (from TileSet metadata).
    /// </summary>
    /// <param name="encoding">Shape encoding ("rect:WxH" or "custom:x1,y1;...")</param>
    /// <param name="width">Bounding box width (must be positive)</param>
    /// <param name="height">Bounding box height (must be positive)</param>
    /// <returns>Result containing ItemShape or parsing/validation error</returns>
    public static Result<ItemShape> CreateFromEncoding(string encoding, int width, int height)
    {
        // BUSINESS RULE: Encoding cannot be empty
        if (string.IsNullOrWhiteSpace(encoding))
            return Result.Failure<ItemShape>("Encoding cannot be empty");

        // BUSINESS RULE: Dimensions must be positive
        if (width <= 0)
            return Result.Failure<ItemShape>("Width must be positive");

        if (height <= 0)
            return Result.Failure<ItemShape>("Height must be positive");

        // Parse encoding format
        if (encoding.StartsWith("rect:", StringComparison.Ordinal))
        {
            return ParseRectangleEncoding(encoding, width, height);
        }
        else if (encoding.StartsWith("custom:", StringComparison.Ordinal))
        {
            return ParseCustomEncoding(encoding, width, height);
        }
        else
        {
            return Result.Failure<ItemShape>("Invalid shape encoding format (expected 'rect:' or 'custom:')");
        }
    }

    /// <summary>
    /// Rotates the shape 90 degrees clockwise.
    /// Transforms OccupiedCells coordinates and swaps Width↔Height.
    /// </summary>
    /// <returns>Result containing rotated ItemShape</returns>
    public Result<ItemShape> RotateClockwise()
    {
        // ROTATION MATH: (x, y) in original W×H → (H-1-y, x) in rotated H×W
        // Example: (0,0) in 2×3 → (2,0) in 3×2
        //          (1,2) in 2×3 → (0,1) in 3×2

        var rotatedCells = OccupiedCells
            .Select(cell => new GridPosition(Height - 1 - cell.Y, cell.X))
            .ToList();

        // Swap dimensions (Width↔Height)
        return Result.Success(new ItemShape(Height, Width, rotatedCells));
    }

    private static Result<ItemShape> ParseRectangleEncoding(string encoding, int width, int height)
    {
        // Format: "rect:WxH"
        var dimensions = encoding.Substring(5); // Skip "rect:"

        if (string.IsNullOrWhiteSpace(dimensions) || !dimensions.Contains('x'))
            return Result.Failure<ItemShape>("Invalid rectangle encoding (expected 'rect:WxH')");

        // For rectangle encoding, just generate all cells (ignore parsed dimensions, use parameters)
        return CreateRectangle(width, height);
    }

    private static Result<ItemShape> ParseCustomEncoding(string encoding, int width, int height)
    {
        // Format: "custom:x1,y1;x2,y2;..."
        var coordsString = encoding.Substring(7); // Skip "custom:"

        // DEBUG: Log input
        System.Console.WriteLine($"[DEBUG ParseCustomEncoding] Input: encoding='{encoding}', width={width}, height={height}");
        System.Console.WriteLine($"[DEBUG ParseCustomEncoding] coordsString='{coordsString}'");

        if (string.IsNullOrWhiteSpace(coordsString))
            return Result.Failure<ItemShape>("Custom encoding requires at least one coordinate");

        var coordinates = coordsString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        System.Console.WriteLine($"[DEBUG ParseCustomEncoding] coordinates.Length={coordinates.Length}");

        if (coordinates.Length == 0)
            return Result.Failure<ItemShape>("Custom encoding requires at least one coordinate");

        var cells = new List<GridPosition>(coordinates.Length);

        foreach (var coord in coordinates)
        {
            var parts = coord.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return Result.Failure<ItemShape>("Invalid coordinate format (expected 'x,y')");

            if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                return Result.Failure<ItemShape>("Invalid coordinate format (expected integers)");

            // BUSINESS RULE: Cells must fit within bounding box
            if (x < 0 || x >= width || y < 0 || y >= height)
                return Result.Failure<ItemShape>($"Cell ({x},{y}) is outside bounding box ({width}×{height})");

            System.Console.WriteLine($"[DEBUG ParseCustomEncoding] Adding cell: ({x},{y})");
            cells.Add(new GridPosition(x, y));
        }

        System.Console.WriteLine($"[DEBUG ParseCustomEncoding] Final cells.Count={cells.Count}");

        // BUSINESS RULE: Must have at least one occupied cell
        if (cells.Count == 0)
            return Result.Failure<ItemShape>("Shape must occupy at least one cell");

        return Result.Success(new ItemShape(width, height, cells));
    }
}
