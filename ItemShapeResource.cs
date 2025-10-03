using Godot;
using System.Linq;

[Tool]
[GlobalClass]
public partial class ItemShapeResource : Resource
{
    private int _width = 2;
    private int _height = 2;

    [Export]
    public int Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                ResizeCellsArray();
            }
        }
    }

    [Export]
    public int Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                ResizeCellsArray();
            }
        }
    }

    // Godot supports int[] for export, use 0/1 for false/true
    // Default: All cells filled (rectangle)
    [Export] public int[] Cells { get; set; } = new int[] { 1, 1, 1, 1 };

    // Auto-resize cells array when Width or Height changes
    private void ResizeCellsArray()
    {
        int expectedSize = _width * _height;

        if (Cells == null || Cells.Length != expectedSize)
        {
            // Preserve existing cell values where possible
            var oldCells = Cells ?? new int[0];
            Cells = new int[expectedSize];

            // Default: Fill all cells (rectangle)
            for (int i = 0; i < expectedSize; i++)
            {
                Cells[i] = 1;
            }

            // Copy old values that still fit
            int copyCount = System.Math.Min(oldCells.Length, expectedSize);
            for (int i = 0; i < copyCount; i++)
            {
                Cells[i] = oldCells[i];
            }

            NotifyPropertyListChanged();
        }
    }

    // Convert to domain encoding string
    public string ToEncoding()
    {
        // Check if it's a simple rectangle (all cells filled)
        if (Cells.All(c => c == 1))
            return $"rect:{Width}x{Height}";

        // Complex shape: Export relative coordinates
        var coords = new System.Collections.Generic.List<string>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index = y * Width + x;
                if (index < Cells.Length && Cells[index] == 1)
                    coords.Add($"{x},{y}");
            }
        }
        return $"custom:{string.Join(";", coords)}";
    }
}
