using Darklands.Application.Common;
using Darklands.Application.Infrastructure.Debug;
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Darklands;

/// <summary>
/// Debug window UI that provides runtime access to debug configuration settings.
/// Features collapsible categories, search functionality, and "Toggle All" controls.
/// Automatically generates UI elements from DebugConfig properties using reflection.
/// Positioned at (20, 20) with size (350, 500) for easy access without covering game view.
/// </summary>
public partial class DebugWindow : Window
{
    private readonly DebugConfig _config;
    private VBoxContainer _mainContainer = null!;
    private LineEdit _searchBox = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _categoriesContainer = null!;
    private Label _titleLabel = null!;

    /// <summary>
    /// Base font sizes for scaling calculations.
    /// Increased default sizes for better readability during debugging.
    /// </summary>
    private const int BaseTitleFontSize = 18;
    private const int BaseRegularFontSize = 16;
    private const int BaseWindowWidth = 350;

    /// <summary>
    /// Maps category names to their expanded/collapsed state.
    /// Persists category visibility across debug window toggles.
    /// </summary>
    private readonly Dictionary<string, bool> _categoryExpanded = new();

    /// <summary>
    /// Maps category names to their container nodes for dynamic show/hide.
    /// Used for search filtering and category management.
    /// </summary>
    private readonly Dictionary<string, Control> _categoryContainers = new();

    public DebugWindow(DebugConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        SetupWindowProperties();
        CreateUI();
        PopulateCategories();

        // Set up responsive font scaling with persistent font size
        SizeChanged += OnWindowSizeChanged;
        
        // Apply initial font sizing from configuration
        ApplyPersistentFontSettings();
    }

    /// <summary>
    /// Configures window properties for optimal debug access.
    /// Positioned to not interfere with game view while remaining easily accessible.
    /// </summary>
    private void SetupWindowProperties()
    {
        Title = "Debug Configuration";
        
        // Load persistent settings from configuration
        Size = _config.DebugWindowSize;
        Position = _config.DebugWindowPosition;

        // Enable resizing with sensible constraints
        Unresizable = false;
        MinSize = new Vector2I(300, 400);  // Minimum size for usability
        MaxSize = new Vector2I(600, 800);  // Maximum size to prevent excessive screen usage

        // Keep window on top but not always
        Transient = false;

        // Enable close button and handle close requests
        CloseRequested += () =>
        {
            SaveWindowState();
            Visible = false;
            // Notify debug system that window was closed via close button
            if (DebugSystem.Instance != null)
            {
                DebugSystem.Instance.Logger.Log(LogLevel.Information, LogCategory.Developer, "Debug window closed via close button");
            }
        };
        
        // Save state when window is moved or resized
        SizeChanged += SaveWindowState;
    }

    /// <summary>
    /// Creates the main UI structure with search box and scrollable category container.
    /// Uses VBoxContainer for clean vertical layout and ScrollContainer for many options.
    /// </summary>
    private void CreateUI()
    {
        _mainContainer = new VBoxContainer();
        AddChild(_mainContainer);

        // Set to fill the entire window
        _mainContainer.AnchorLeft = 0;
        _mainContainer.AnchorTop = 0;
        _mainContainer.AnchorRight = 1;
        _mainContainer.AnchorBottom = 1;

        // Add title label
        _titleLabel = new Label
        {
            Text = "🐛 Debug Configuration",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        _mainContainer.AddChild(_titleLabel);

        // Add search box
        _searchBox = new LineEdit
        {
            PlaceholderText = "🔍 Search settings..."
        };
        _searchBox.TextChanged += OnSearchTextChanged;
        _mainContainer.AddChild(_searchBox);

        // Add log level selection
        var logLevelContainer = new HBoxContainer();
        _mainContainer.AddChild(logLevelContainer);

        logLevelContainer.AddChild(new Label { Text = "Log Level:" });

        var logLevelOption = new OptionButton();
        logLevelOption.AddItem("Debug", (int)LogLevel.Debug);
        logLevelOption.AddItem("Information", (int)LogLevel.Information);
        logLevelOption.AddItem("Warning", (int)LogLevel.Warning);
        logLevelOption.AddItem("Error", (int)LogLevel.Error);

        // Set current selection
        logLevelOption.Selected = (int)_config.CurrentLogLevel;

        logLevelOption.ItemSelected += (long index) =>
        {
            _config.CurrentLogLevel = (LogLevel)index;
            _config.NotifySettingChanged(nameof(_config.CurrentLogLevel));
        };

        logLevelContainer.AddChild(logLevelOption);

        // Add font size control
        var fontSizeContainer = new HBoxContainer();
        _mainContainer.AddChild(fontSizeContainer);

        fontSizeContainer.AddChild(new Label { Text = "Font Size:" });

        var fontSizeSpinBox = new SpinBox
        {
            MinValue = 10,
            MaxValue = 24,
            Step = 1,
            Value = _config.DebugWindowFontSize
        };

        fontSizeSpinBox.ValueChanged += (double value) =>
        {
            _config.DebugWindowFontSize = (int)value;
            _config.NotifySettingChanged(nameof(_config.DebugWindowFontSize));
            ApplyPersistentFontSettings();
        };

        fontSizeContainer.AddChild(fontSizeSpinBox);

        // Add separator
        _mainContainer.AddChild(new HSeparator());

        // Create scroll container for categories
        _scrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _mainContainer.AddChild(_scrollContainer);

        // Container for all categories
        _categoriesContainer = new VBoxContainer();
        _scrollContainer.AddChild(_categoriesContainer);
    }

    /// <summary>
    /// Automatically generates UI categories from DebugConfig ExportGroup attributes.
    /// Uses reflection to discover categories and their properties.
    /// Creates collapsible sections with toggle-all functionality.
    /// </summary>
    private void PopulateCategories()
    {
        var categories = GetDebugConfigCategories();

        foreach (var category in categories)
        {
            CreateCategorySection(category.Key, category.Value);
        }
    }

    /// <summary>
    /// Extracts categories from DebugConfig using ExportGroup attributes.
    /// Maps category names to their properties for UI generation.
    /// </summary>
    /// <returns>Dictionary mapping category names to their properties</returns>
    private Dictionary<string, List<PropertyInfo>> GetDebugConfigCategories()
    {
        var categories = new Dictionary<string, List<PropertyInfo>>();
        var properties = _config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        string currentCategory = "General";

        foreach (var property in properties)
        {
            // Check for ExportGroup attribute to determine category
            var exportGroupAttr = property.GetCustomAttribute<ExportGroupAttribute>();
            if (exportGroupAttr != null)
            {
                currentCategory = exportGroupAttr.Name;
                continue;
            }

            // Check if property has Export attribute (should be included in UI)
            var exportAttr = property.GetCustomAttribute<ExportAttribute>();
            if (exportAttr != null && property.PropertyType == typeof(bool))
            {
                if (!categories.ContainsKey(currentCategory))
                    categories[currentCategory] = new List<PropertyInfo>();

                categories[currentCategory].Add(property);
            }
        }

        return categories;
    }

    /// <summary>
    /// Creates a collapsible section for a debug category.
    /// Includes expand/collapse header, toggle-all checkbox, and individual property controls.
    /// </summary>
    /// <param name="categoryName">Name of the category to create</param>
    /// <param name="properties">Properties belonging to this category</param>
    private void CreateCategorySection(string categoryName, List<PropertyInfo> properties)
    {
        // Default categories to expanded
        _categoryExpanded[categoryName] = true;

        // Category header button
        var headerButton = new Button
        {
            Text = $"▼ {categoryName}",
            Flat = true,
            Alignment = HorizontalAlignment.Left
        };

        headerButton.Pressed += () => ToggleCategory(categoryName, headerButton);
        _categoriesContainer.AddChild(headerButton);

        // Category container
        var categoryContainer = new VBoxContainer();
        _categoriesContainer.AddChild(categoryContainer);
        _categoryContainers[categoryName] = categoryContainer;

        // Add some indentation
        categoryContainer.AddThemeConstantOverride("margin_left", 20);

        // Toggle All checkbox
        var toggleAllCheckbox = new CheckBox
        {
            Text = "Enable All",
            ButtonPressed = AreAllCategorySettingsEnabled(properties)
        };

        toggleAllCheckbox.Toggled += (bool pressed) => ToggleAllInCategory(properties, pressed);
        categoryContainer.AddChild(toggleAllCheckbox);

        // Individual property checkboxes
        foreach (var property in properties)
        {
            CreatePropertyCheckbox(categoryContainer, property);
        }

        // Add separator between categories
        _categoriesContainer.AddChild(new HSeparator());
    }

    /// <summary>
    /// Creates a checkbox control for a boolean property.
    /// Automatically syncs with the DebugConfig property value.
    /// </summary>
    /// <param name="parent">Parent container to add the checkbox to</param>
    /// <param name="property">Property info for the boolean setting</param>
    private void CreatePropertyCheckbox(Container parent, PropertyInfo property)
    {
        var checkbox = new CheckBox
        {
            Text = FormatPropertyName(property.Name),
            ButtonPressed = (bool)(property.GetValue(_config) ?? false)
        };

        checkbox.Toggled += (bool pressed) =>
        {
            property.SetValue(_config, pressed);
            _config.NotifySettingChanged(property.Name);
        };

        parent.AddChild(checkbox);
    }

    /// <summary>
    /// Converts property names from PascalCase to human-readable format.
    /// Example: "ShowVisionRanges" becomes "Show Vision Ranges"
    /// </summary>
    /// <param name="propertyName">Property name in PascalCase</param>
    /// <returns>Human-readable property name</returns>
    private string FormatPropertyName(string propertyName)
    {
        var result = "";
        for (int i = 0; i < propertyName.Length; i++)
        {
            if (i > 0 && char.IsUpper(propertyName[i]))
                result += " ";
            result += propertyName[i];
        }
        return result;
    }

    /// <summary>
    /// Toggles the expanded/collapsed state of a category.
    /// Updates button text and container visibility.
    /// </summary>
    /// <param name="categoryName">Name of the category to toggle</param>
    /// <param name="headerButton">Header button to update text for</param>
    private void ToggleCategory(string categoryName, Button headerButton)
    {
        _categoryExpanded[categoryName] = !_categoryExpanded[categoryName];
        var isExpanded = _categoryExpanded[categoryName];

        headerButton.Text = $"{(isExpanded ? "▼" : "▶")} {categoryName}";

        if (_categoryContainers.TryGetValue(categoryName, out var container))
        {
            container.Visible = isExpanded;
        }
    }

    /// <summary>
    /// Toggles all boolean properties in a category.
    /// Used by the "Enable All" checkbox functionality.
    /// </summary>
    /// <param name="properties">Properties to toggle</param>
    /// <param name="enabled">Whether to enable or disable all properties</param>
    private void ToggleAllInCategory(List<PropertyInfo> properties, bool enabled)
    {
        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(bool))
            {
                property.SetValue(_config, enabled);
                _config.NotifySettingChanged(property.Name);
            }
        }

        // Refresh UI to reflect changes
        RefreshAllCheckboxes();
    }

    /// <summary>
    /// Checks if all settings in a category are currently enabled.
    /// Used to set the initial state of "Enable All" checkboxes.
    /// </summary>
    /// <param name="properties">Properties to check</param>
    /// <returns>True if all boolean properties are enabled</returns>
    private bool AreAllCategorySettingsEnabled(List<PropertyInfo> properties)
    {
        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(bool))
            {
                if (!(bool)(property.GetValue(_config) ?? false))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Refreshes all checkbox states to match current configuration.
    /// Called after batch updates to ensure UI consistency.
    /// </summary>
    private void RefreshAllCheckboxes()
    {
        // This would require keeping references to all checkboxes
        // For now, the individual checkbox event handlers maintain consistency
        // A full implementation would store checkbox references for batch updates
    }

    /// <summary>
    /// Handles search text changes to filter visible categories and settings.
    /// Shows/hides categories based on search term matching.
    /// </summary>
    /// <param name="searchText">Text to search for in setting names</param>
    private void OnSearchTextChanged(string searchText)
    {
        // Simple search implementation - hide categories that don't match
        // A more sophisticated implementation would highlight matches
        foreach (var kvp in _categoryContainers)
        {
            var categoryName = kvp.Key;
            var container = kvp.Value;

            bool shouldShow = string.IsNullOrEmpty(searchText) ||
                            categoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

            ((Control)container.GetParent()).Visible = shouldShow;
        }
    }

    /// <summary>
    /// Handles window size changes to save state and maintain font consistency.
    /// Uses persistent font settings instead of scaling calculations.
    /// </summary>
    private void OnWindowSizeChanged()
    {
        // Save window state when resized
        SaveWindowState();
        
        // Apply persistent font settings (no scaling)
        ApplyPersistentFontSettings();
    }


    /// <summary>
    /// Recursively updates font sizes for all child controls.
    /// Ensures consistent font scaling throughout the debug window.
    /// </summary>
    /// <param name="parent">Parent container to update</param>
    /// <param name="fontSize">Font size to apply to child controls</param>
    private void UpdateChildFontSizes(Node parent, int fontSize)
    {
        if (parent == null) return;

        for (int i = 0; i < parent.GetChildCount(); i++)
        {
            var child = parent.GetChild(i);

            // Update font sizes for text-displaying controls
            switch (child)
            {
                case Label label:
                    label.AddThemeFontSizeOverride("font_size", fontSize);
                    break;
                case CheckBox checkBox:
                    checkBox.AddThemeFontSizeOverride("font_size", fontSize);
                    break;
                case OptionButton optionButton:
                    optionButton.AddThemeFontSizeOverride("font_size", fontSize);
                    break;
                case LineEdit lineEdit:
                    lineEdit.AddThemeFontSizeOverride("font_size", fontSize);
                    break;
                case Button button:
                    button.AddThemeFontSizeOverride("font_size", fontSize);
                    break;
            }

            // Recursively update children
            UpdateChildFontSizes(child, fontSize);
        }
    }
    
    /// <summary>
    /// Saves current window position and size to persistent configuration.
    /// Called automatically when window is closed or resized.
    /// </summary>
    private void SaveWindowState()
    {
        _config.DebugWindowSize = Size;
        _config.DebugWindowPosition = Position;
        _config.NotifySettingChanged(nameof(_config.DebugWindowSize));
        _config.NotifySettingChanged(nameof(_config.DebugWindowPosition));
    }
    
    /// <summary>
    /// Applies persistent font settings from configuration.
    /// Uses configured font size instead of scaling calculations.
    /// </summary>
    private void ApplyPersistentFontSettings()
    {
        var fontSize = _config.DebugWindowFontSize;
        var titleFontSize = fontSize + 2; // Title slightly larger
        
        // Update title font size
        if (_titleLabel != null)
        {
            _titleLabel.AddThemeFontSizeOverride("font_size", titleFontSize);
        }

        // Update search box font size
        if (_searchBox != null)
        {
            _searchBox.AddThemeFontSizeOverride("font_size", fontSize);
        }

        // Update all category labels and checkboxes
        UpdateChildFontSizes(_categoriesContainer, fontSize);
    }
}
