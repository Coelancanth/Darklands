using Godot;

[Tool]
public partial class ItemShapeEditorPlugin : EditorPlugin
{
    private ItemShapeInspectorPlugin? _inspectorPlugin;

    public override void _EnterTree()
    {
        GD.Print("Item Shape Editor Plugin Loaded!");
        _inspectorPlugin = new ItemShapeInspectorPlugin();
        AddInspectorPlugin(_inspectorPlugin);
    }

    public override void _ExitTree()
    {
        GD.Print("Item Shape Editor Plugin Unloaded!");
        if (_inspectorPlugin != null)
        {
            RemoveInspectorPlugin(_inspectorPlugin);
        }
    }
}

public partial class ItemShapeInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object)
    {
        return @object is ItemShapeResource;
    }

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name,
        PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
    {
        if (name == "Cells")
        {
            var editor = new ShapeGridEditor();
            if (@object is ItemShapeResource resource)
            {
                editor.SetObject(resource);
            }
            AddPropertyEditor(name, editor);
            return true; // Hide default array display
        }
        return false;
    }
}

public partial class ShapeGridEditor : EditorProperty
{
    private ItemShapeResource? _resource;
    private GridContainer? _grid;
    private CheckBox[]? _checkboxes;

    public void SetObject(ItemShapeResource? resource)
    {
        _resource = resource;
    }

    public override void _Ready()
    {
        // Create grid container for checkboxes
        _grid = new GridContainer
        {
            CustomMinimumSize = new Vector2(0, 100)
        };
        AddChild(_grid);

        // Initial render
        RebuildGrid();

        // Listen for Width/Height changes
        if (_resource != null)
        {
            _resource.PropertyListChanged += RebuildGrid;
        }
    }

    private void RebuildGrid()
    {
        if (_resource == null || _grid == null)
            return;

        // Clear existing checkboxes
        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        // Set grid columns
        _grid.Columns = _resource.Width;

        // Create checkboxes for each cell
        int cellCount = _resource.Width * _resource.Height;
        _checkboxes = new CheckBox[cellCount];

        for (int i = 0; i < cellCount; i++)
        {
            var checkbox = new CheckBox
            {
                ButtonPressed = i < _resource.Cells.Length && _resource.Cells[i] == 1,
                TooltipText = $"Cell [{i}] (Row {i / _resource.Width}, Col {i % _resource.Width})"
            };

            int index = i; // Capture for closure
            checkbox.Toggled += (pressed) => OnCellToggled(index, pressed);

            _grid.AddChild(checkbox);
            _checkboxes[i] = checkbox;
        }
    }

    private void OnCellToggled(int index, bool pressed)
    {
        if (_resource == null || index >= _resource.Cells.Length)
            return;

        // Update resource data (int array: 0=unchecked, 1=checked)
        _resource.Cells[index] = pressed ? 1 : 0;

        // Notify Godot that property changed (for undo/redo)
        EmitChanged("Cells", Variant.From(_resource.Cells));
    }

    public override void _ExitTree()
    {
        if (_resource != null)
        {
            _resource.PropertyListChanged -= RebuildGrid;
        }
    }
}
