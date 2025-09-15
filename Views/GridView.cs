using Darklands.Core.Presentation.Views;
using Darklands.Core.Presentation.Presenters;
using Darklands.Core.Domain.Debug;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darklands.Views
{
    /// <summary>
    /// Concrete Godot implementation of the grid view interface.
    /// Handles the visual representation of the tactical combat grid using simple ColorRect tiles.
    /// Manages tile rendering, highlighting, and user input detection with pure colors.
    /// </summary>
    public partial class GridView : Node2D, IGridView
    {
        private GridPresenter? _presenter;
        private ICategoryLogger? _logger;
        private const int TileSize = 64;
        private int _gridWidth;
        private int _gridHeight;
        private Dictionary<Vector2I, ColorRect> _tiles = new();
        private readonly List<Line2D> _gridLines = new();

        // Fog of war state management
        private Darklands.Core.Domain.Vision.VisionState? _currentVisionState;

        // Colors for different terrain types
        private readonly Color GrassColor = new Color(0.3f, 0.7f, 0.2f); // Green
        private readonly Color StoneColor = new Color(0.5f, 0.5f, 0.5f); // Gray
        private readonly Color WaterColor = new Color(0.2f, 0.4f, 0.8f); // Blue
        private readonly Color ForestColor = new Color(0.1f, 0.4f, 0.1f); // Dark Green
        private readonly Color HighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.7f); // Yellow with transparency
        private readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark gray with transparency
        private const float GridLineWidth = 1.0f;

        // Fog of war colors (applied as modulation to preserve terrain colors)
        private readonly Color FogUnseen = new Color(0.05f, 0.05f, 0.05f);       // Dark fog (unseen areas)
        private readonly Color FogExplored = new Color(0.4f, 0.4f, 0.4f);    // Medium gray (previously explored)
        private readonly Color FogVisible = new Color(1.0f, 1.0f, 1.0f);      // White (no modulation - fully visible)

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Initializes the grid display system with ColorRect-based tiles.
        /// </summary>
        public override void _Ready()
        {
            try
            {
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "GridView._Ready error: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Handles input events for grid interaction.
        /// Detects mouse clicks on tiles and converts screen coordinates to grid positions.
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            try
            {
                if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
                {
                    if (mouseEvent.ButtonIndex == MouseButton.Left)
                    {
                        HandleMouseClick(mouseEvent.GlobalPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.System, "GridView._Input error: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Sets the presenter that controls this view.
        /// Called during initialization to establish the MVP connection.
        /// </summary>
        public void SetPresenter(GridPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        /// <summary>
        /// Sets the logger for this view.
        /// Called during initialization to enable proper logging.
        /// </summary>
        public void SetLogger(ICategoryLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Displays the grid boundaries with the specified dimensions.
        /// Creates the visual grid using ColorRect tiles and grid lines.
        /// </summary>
        public async Task DisplayGridBoundariesAsync(int width, int height)
        {
            _gridWidth = width;
            _gridHeight = height;

            // Clear any existing tiles and grid lines
            ClearAllTiles();
            ClearGridLines();

            _logger?.Log(LogLevel.Information, LogCategory.System, "Creating {Width}x{Height} grid with lines", width, height);

            // Create a basic grid with grass tiles as default, with initial fog applied
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tilePosition = new Vector2I(x, y);
                    CreateTileWithInitialFog(tilePosition, GrassColor);
                }
            }

            // Create grid lines to separate tiles
            CreateGridLines(width, height);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates the entire grid display with the current tile states.
        /// Renders all tiles according to their terrain types and occupancy.
        /// </summary>
        public async Task RefreshGridAsync(Darklands.Core.Domain.Grid.Grid grid)
        {
            _gridWidth = grid.Width;
            _gridHeight = grid.Height;

            // Clear existing tiles and grid lines
            ClearAllTiles();
            ClearGridLines();

            // Render each tile based on its state
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var position = new Darklands.Core.Domain.Grid.Position(x, y);
                    var tileResult = grid.GetTile(position);

                    tileResult.Match(
                        Succ: tile =>
                        {
                            var tileColor = GetColorFromTerrain(tile.TerrainType);
                            var tilePosition = new Vector2I(x, y);
                            CreateTileWithInitialFog(tilePosition, tileColor);
                        },
                        Fail: _ =>
                        {
                            // Use default grass color for invalid positions
                            var tilePosition = new Vector2I(x, y);
                            CreateTileWithInitialFog(tilePosition, GrassColor);
                        }
                    );
                }
            }

            // Create grid lines to separate tiles
            CreateGridLines(grid.Width, grid.Height);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates a single tile's visual representation.
        /// </summary>
        public async Task UpdateTileAsync(Darklands.Core.Domain.Grid.Position position, Darklands.Core.Domain.Grid.Tile tile)
        {
            var tileColor = GetColorFromTerrain(tile.TerrainType);
            var tilePosition = new Vector2I(position.X, position.Y);
            UpdateTileColor(tilePosition, tileColor);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Highlights a tile to indicate selection, hover, or special state.
        /// </summary>
        public async Task HighlightTileAsync(Darklands.Core.Domain.Grid.Position position, HighlightType highlightType)
        {
            // For Phase 4, use a simple color overlay
            // Future: Different highlight types could use different visual effects
            var tilePosition = new Vector2I(position.X, position.Y);
            UpdateTileColor(tilePosition, HighlightColor);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Removes highlighting from a tile.
        /// </summary>
        public async Task UnhighlightTileAsync(Darklands.Core.Domain.Grid.Position position)
        {
            // Restore original tile color (assume grass for now)
            var tilePosition = new Vector2I(position.X, position.Y);
            UpdateTileColor(tilePosition, GrassColor);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Shows visual feedback for successful operations.
        /// </summary>
        public async Task ShowSuccessFeedbackAsync(Darklands.Core.Domain.Grid.Position position, string message)
        {
            // For Phase 4, just print to console
            // Future: Show floating text or particle effects
            _logger?.Log(LogLevel.Debug, LogCategory.System, "{Message} at ({X},{Y})", message, position.X, position.Y);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Shows visual feedback for invalid operations or errors.
        /// </summary>
        public async Task ShowErrorFeedbackAsync(Darklands.Core.Domain.Grid.Position position, string errorMessage)
        {
            // For Phase 4, just print to console
            // Future: Show error indicators or red highlights
            _logger?.Log(LogLevel.Warning, LogCategory.System, "{ErrorMessage} at ({X},{Y})", errorMessage, position.X, position.Y);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears all visual overlays and resets the grid to clean state.
        /// </summary>
        public async Task ClearOverlaysAsync()
        {
            // Reset all tiles to their default grass color
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    var tilePosition = new Vector2I(x, y);
                    UpdateTileColor(tilePosition, GrassColor);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates the fog of war display based on a vision state.
        /// Applies modulation to all tiles to show visibility levels while preserving terrain colors.
        /// </summary>
        /// <param name="visionState">The vision state containing visibility information</param>
        public async Task UpdateFogOfWarAsync(Darklands.Core.Domain.Vision.VisionState visionState)
        {
            _logger?.Log(LogLevel.Debug, LogCategory.Vision, "GridView.UpdateFogOfWarAsync called for Actor {ActorId}: {Visible} visible, {Explored} explored",
                visionState.ViewerId.Value.ToString()[..8],
                visionState.CurrentlyVisible.Count,
                visionState.PreviouslyExplored.Count);

            _currentVisionState = visionState;
            _logger?.Log(LogLevel.Debug, LogCategory.Vision, "GridView: Calling deferred fog update for {GridWidth}x{GridHeight} grid", _gridWidth, _gridHeight);
            CallDeferred(nameof(UpdateFogOfWarDeferred));

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates the fog state for a single tile.
        /// Useful for incremental updates when vision changes.
        /// </summary>
        /// <param name="position">Grid position of the tile</param>
        /// <param name="visibilityLevel">New visibility level for the tile</param>
        public async Task UpdateTileFogAsync(Darklands.Core.Domain.Grid.Position position, Darklands.Core.Domain.Vision.VisibilityLevel visibilityLevel)
        {
            var fogColor = GetFogColorFromVisibility(visibilityLevel);
            var tilePosition = new Vector2I(position.X, position.Y);

            CallDeferred(nameof(UpdateTileFogDeferred), tilePosition, fogColor);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears all fog of war effects, making all tiles fully visible.
        /// </summary>
        public async Task ClearFogOfWarAsync()
        {
            _logger?.Log(LogLevel.Debug, LogCategory.Vision, "Clearing all fog of war effects");
            _currentVisionState = null;
            CallDeferred(nameof(ClearFogOfWarDeferred));

            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles mouse clicks on the grid by converting screen coordinates to grid positions.
        /// </summary>
        private void HandleMouseClick(Vector2 globalPosition)
        {
            try
            {
                if (_presenter == null) return;

                // Convert global position to local position relative to this node
                var localPosition = ToLocal(globalPosition);

                // Calculate tile position based on tile size
                var tileX = (int)(localPosition.X / TileSize);
                var tileY = (int)(localPosition.Y / TileSize);

                // Validate that the click is within grid bounds
                if (tileX >= 0 && tileX < _gridWidth &&
                    tileY >= 0 && tileY < _gridHeight)
                {
                    var gridPosition = new Darklands.Core.Domain.Grid.Position(tileX, tileY);

                    _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "User clicked tile ({X},{Y})", tileX, tileY);

                    // Notify the presenter about the tile click - Use CallDeferred for main-thread safety
                    CallDeferred(nameof(HandleTileClickDeferred), gridPosition.X, gridPosition.Y);
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "HandleMouseClick error: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Deferred handler for tile clicks to ensure main-thread execution and sequential processing.
        /// Converts async presenter call to synchronous per ADR-009.
        /// </summary>
        private void HandleTileClickDeferred(int x, int y)
        {
            try
            {
                var gridPosition = new Darklands.Core.Domain.Grid.Position(x, y);
                // Convert async call to synchronous per ADR-009 Sequential Turn Processing
                _presenter?.HandleTileClickAsync(gridPosition).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error handling deferred tile click at ({X},{Y}): {Error}",
                    x, y, ex.Message);
            }
        }

        /// <summary>
        /// Creates a ColorRect tile at the specified position with the given color.
        /// </summary>
        private void CreateTile(Vector2I position, Color color)
        {
            var tile = new ColorRect
            {
                Size = new Vector2(TileSize, TileSize),
                Position = new Vector2(position.X * TileSize, position.Y * TileSize),
                Color = color
            };

            CallDeferred(MethodName.AddTileToScene, tile, position);
        }

        /// <summary>
        /// Creates a ColorRect tile with initial fog applied (all tiles start as unseen).
        /// </summary>
        private void CreateTileWithInitialFog(Vector2I position, Color color)
        {
            var tile = new ColorRect
            {
                Size = new Vector2(TileSize, TileSize),
                Position = new Vector2(position.X * TileSize, position.Y * TileSize),
                Color = color,
                Modulate = FogUnseen // Apply initial fog - all tiles start as unseen
            };

            CallDeferred(MethodName.AddTileToScene, tile, position);

            // Debug log for first few tiles to confirm initial fog application
            if (position.X < 3 && position.Y < 3)
            {
                _logger?.Log(LogLevel.Debug, LogCategory.Vision, "GridView: Created tile ({X},{Y}) with initial fog - Color: {TerrainColor}, Fog: {FogColor}",
                    position.X, position.Y, color, FogUnseen);
            }
        }

        /// <summary>
        /// Helper method to add tile to scene on main thread.
        /// </summary>
        private void AddTileToScene(ColorRect tile, Vector2I position)
        {
            AddChild(tile);
            _tiles[position] = tile;
        }

        /// <summary>
        /// Updates the color of an existing tile.
        /// </summary>
        private void UpdateTileColor(Vector2I position, Color color)
        {
            CallDeferred(MethodName.UpdateTileColorDeferred, position, color);
        }

        /// <summary>
        /// Helper method to update tile color on main thread.
        /// </summary>
        private void UpdateTileColorDeferred(Vector2I position, Color color)
        {
            if (_tiles.TryGetValue(position, out var tile))
            {
                tile.Color = color;
            }
        }

        /// <summary>
        /// Clears all tiles from the grid.
        /// </summary>
        private void ClearAllTiles()
        {
            CallDeferred(MethodName.ClearAllTilesDeferred);
        }

        /// <summary>
        /// Helper method to clear tiles on main thread.
        /// </summary>
        private void ClearAllTilesDeferred()
        {
            foreach (var tile in _tiles.Values)
            {
                tile?.QueueFree();
            }
            _tiles.Clear();
        }

        /// <summary>
        /// Converts terrain type to color for rendering.
        /// </summary>
        private Color GetColorFromTerrain(Darklands.Core.Domain.Grid.TerrainType terrain)
        {
            return terrain switch
            {
                Darklands.Core.Domain.Grid.TerrainType.Open => GrassColor,
                Darklands.Core.Domain.Grid.TerrainType.Rocky => StoneColor,
                Darklands.Core.Domain.Grid.TerrainType.Water => WaterColor,
                Darklands.Core.Domain.Grid.TerrainType.Forest => ForestColor,
                Darklands.Core.Domain.Grid.TerrainType.Hill => StoneColor,
                Darklands.Core.Domain.Grid.TerrainType.Swamp => WaterColor,
                Darklands.Core.Domain.Grid.TerrainType.Wall => StoneColor,
                _ => GrassColor // Default to grass
            };
        }

        /// <summary>
        /// Creates grid lines to visually separate tiles.
        /// Draws both horizontal and vertical lines across the grid.
        /// </summary>
        private void CreateGridLines(int width, int height)
        {
            CallDeferred(MethodName.CreateGridLinesDeferred, width, height);
        }

        /// <summary>
        /// Helper method to create grid lines on main thread.
        /// </summary>
        private void CreateGridLinesDeferred(int width, int height)
        {
            // Create vertical lines
            for (int x = 0; x <= width; x++)
            {
                var verticalLine = new Line2D
                {
                    DefaultColor = GridLineColor,
                    Width = GridLineWidth
                };

                // Line from top to bottom of grid
                var startPoint = new Vector2(x * TileSize, 0);
                var endPoint = new Vector2(x * TileSize, height * TileSize);

                verticalLine.AddPoint(startPoint);
                verticalLine.AddPoint(endPoint);

                AddChild(verticalLine);
                _gridLines.Add(verticalLine);
            }

            // Create horizontal lines
            for (int y = 0; y <= height; y++)
            {
                var horizontalLine = new Line2D
                {
                    DefaultColor = GridLineColor,
                    Width = GridLineWidth
                };

                // Line from left to right of grid
                var startPoint = new Vector2(0, y * TileSize);
                var endPoint = new Vector2(width * TileSize, y * TileSize);

                horizontalLine.AddPoint(startPoint);
                horizontalLine.AddPoint(endPoint);

                AddChild(horizontalLine);
                _gridLines.Add(horizontalLine);
            }
        }

        /// <summary>
        /// Clears all grid lines from the display.
        /// </summary>
        private void ClearGridLines()
        {
            CallDeferred(MethodName.ClearGridLinesDeferred);
        }

        /// <summary>
        /// Helper method to clear grid lines on main thread.
        /// </summary>
        private void ClearGridLinesDeferred()
        {
            foreach (var line in _gridLines)
            {
                line?.QueueFree();
            }
            _gridLines.Clear();
        }

        /// <summary>
        /// Creates a strategic 30x20 test grid layout for fog of war and vision testing.
        /// Designed for 4K displays (1920x1280 pixels at 64px/tile) with comprehensive tactical scenarios.
        /// Features: Long walls, pillar formations, corridors, and room structures for shadowcasting validation.
        /// </summary>
        /// <returns>Strategic test grid with player at center (15,10) and complex terrain</returns>
        public static Darklands.Core.Domain.Grid.Grid CreateStrategicTestGrid(Core.Domain.Common.IStableIdGenerator idGenerator)
        {
            const int width = 30;
            const int height = 20;

            // Create base empty grid (Open terrain)
            var grid = Darklands.Core.Domain.Grid.Grid.Create(idGenerator, width, height, Darklands.Core.Domain.Grid.TerrainType.Open)
                .IfFail(_ => throw new System.InvalidOperationException("Failed to create strategic test grid"));

            // Strategic Layout Design:
            // - Player at (15, 10) - center position with vision range 8
            // - Complex wall patterns for comprehensive shadowcasting testing
            // - Pillar formations for corner occlusion validation
            // - Room structures with corridors for tactical movement

            // === PERIMETER WALLS (Frame) ===
            // Top and bottom borders
            for (int x = 0; x < width; x++)
            {
                grid = PlaceWall(grid, x, 0);      // Top border
                grid = PlaceWall(grid, x, height - 1); // Bottom border
            }
            // Left and right borders  
            for (int y = 0; y < height; y++)
            {
                grid = PlaceWall(grid, 0, y);         // Left border
                grid = PlaceWall(grid, width - 1, y); // Right border
            }

            // === MAJOR STRUCTURAL WALLS ===
            // Long horizontal wall for shadowcasting validation (with gaps)
            for (int x = 3; x <= 12; x++)
            {
                grid = PlaceWall(grid, x, 6);
            }
            // Gap at (13, 6) for corridor access
            for (int x = 14; x <= 26; x++)
            {
                grid = PlaceWall(grid, x, 6);
            }

            // Vertical dividing wall with strategic gaps
            for (int y = 2; y <= 5; y++)
            {
                grid = PlaceWall(grid, 8, y);
            }
            // Gap at (8, 6) already created by horizontal wall intersection
            for (int y = 7; y <= 10; y++)
            {
                grid = PlaceWall(grid, 8, y);
            }
            // Gap at (8, 11) for access
            for (int y = 12; y <= 17; y++)
            {
                grid = PlaceWall(grid, 8, y);
            }

            // === ROOM STRUCTURES ===
            // Northwest room (closed with single entrance)
            for (int x = 2; x <= 6; x++)
            {
                grid = PlaceWall(grid, x, 2);
                grid = PlaceWall(grid, x, 4);
            }
            for (int y = 2; y <= 4; y++)
            {
                grid = PlaceWall(grid, 2, y);
                grid = PlaceWall(grid, 6, y);
            }
            // Entrance at (4, 4) - remove wall
            grid = PlaceOpen(grid, 4, 4);

            // Northeast room (L-shaped)
            for (int x = 18; x <= 22; x++)
            {
                grid = PlaceWall(grid, x, 2);
            }
            for (int y = 2; y <= 4; y++)
            {
                grid = PlaceWall(grid, 18, y);
                grid = PlaceWall(grid, 22, y);
            }
            grid = PlaceWall(grid, 22, 4);

            // === PILLAR FORMATIONS (Corner occlusion testing) ===
            // Cross formation near player
            grid = PlaceWall(grid, 13, 8);
            grid = PlaceWall(grid, 17, 8);
            grid = PlaceWall(grid, 15, 6);
            grid = PlaceWall(grid, 15, 12);

            // Diagonal pillar line for complex shadows
            grid = PlaceWall(grid, 10, 14);
            grid = PlaceWall(grid, 12, 15);
            grid = PlaceWall(grid, 14, 16);
            grid = PlaceWall(grid, 16, 15);
            grid = PlaceWall(grid, 18, 14);

            // Isolated pillars for corner peeking tests
            grid = PlaceWall(grid, 5, 8);
            grid = PlaceWall(grid, 25, 12);
            grid = PlaceWall(grid, 11, 3);
            grid = PlaceWall(grid, 20, 17);

            // === CORRIDOR SYSTEM ===
            // Main east-west corridor (already created by horizontal wall gap)
            // North-south corridor through center
            for (int y = 8; y <= 12; y++)
            {
                grid = PlaceOpen(grid, 15, y); // Ensure center corridor is open
            }

            // === FOREST AREAS (Additional vision blocking) ===
            // Small forest cluster for varied terrain
            grid = PlaceForest(grid, 24, 8);
            grid = PlaceForest(grid, 25, 8);
            grid = PlaceForest(grid, 24, 9);
            grid = PlaceForest(grid, 25, 9);

            // === SPECIAL TEST POSITIONS ===
            // Ensure player position is open
            grid = PlaceOpen(grid, 15, 10);

            // Monster positions (different vision ranges for testing)
            grid = PlaceOpen(grid, 5, 10);  // Goblin position (range 5)
            grid = PlaceOpen(grid, 20, 15); // Orc position (range 6) 
            grid = PlaceOpen(grid, 25, 5);  // Eagle position (range 12)

            return grid;
        }

        /// <summary>
        /// Helper method to update fog of war on main thread.
        /// Applies fog modulation to all tiles based on stored vision state.
        /// </summary>
        private void UpdateFogOfWarDeferred()
        {
            if (_currentVisionState == null)
            {
                _logger?.Log(LogLevel.Warning, LogCategory.Vision, "GridView.UpdateFogOfWarDeferred: No current vision state available");
                return;
            }

            _logger?.Log(LogLevel.Debug, LogCategory.Vision, "GridView.UpdateFogOfWarDeferred: Processing fog update for {GridWidth}x{GridHeight} grid with {TileCount} tiles",
                _gridWidth, _gridHeight, _tiles.Count);
            _logger?.Log(LogLevel.Debug, LogCategory.Vision, "Vision state details - Currently Visible: {Visible}, Previously Explored: {Explored}",
                _currentVisionState.CurrentlyVisible.Count, _currentVisionState.PreviouslyExplored.Count);

            int visibleCount = 0, exploredCount = 0, unseenCount = 0;

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    var position = new Darklands.Core.Domain.Grid.Position(x, y);
                    var visibilityLevel = _currentVisionState.GetVisibilityLevel(position);
                    var fogColor = GetFogColorFromVisibility(visibilityLevel);
                    var tilePosition = new Vector2I(x, y);

                    // Count visibility levels for debugging
                    switch (visibilityLevel)
                    {
                        case Darklands.Core.Domain.Vision.VisibilityLevel.Visible: visibleCount++; break;
                        case Darklands.Core.Domain.Vision.VisibilityLevel.Explored: exploredCount++; break;
                        case Darklands.Core.Domain.Vision.VisibilityLevel.Unseen: unseenCount++; break;
                    }

                    if (_tiles.TryGetValue(tilePosition, out var tile))
                    {
                        tile.Modulate = fogColor;

                        // Debug a few sample tiles to verify modulation is being applied
                        if (x == 15 && y == 10) // Center position
                        {
                            _logger?.Log(LogLevel.Debug, LogCategory.Vision, "GridView: Applied fog to tile (15,10) - Visibility: {Visibility}, Color: {Color}",
                                visibilityLevel, fogColor);
                        }
                    }
                    else if (x < 5 && y < 5) // Only log for a few tiles to avoid spam
                    {
                        _logger?.Log(LogLevel.Warning, LogCategory.Vision, "GridView: Tile not found at ({X},{Y}) in _tiles dictionary", x, y);
                    }
                }
            }

            _logger?.Log(LogLevel.Debug, LogCategory.Vision, "GridView: Fog update complete - {Visible} visible, {Explored} explored, {Unseen} unseen tiles",
                visibleCount, exploredCount, unseenCount);
        }

        /// <summary>
        /// Helper method to update single tile fog on main thread.
        /// Applies fog modulation to preserve terrain color while showing visibility.
        /// </summary>
        private void UpdateTileFogDeferred(Vector2I position, Color fogColor)
        {
            if (_tiles.TryGetValue(position, out var tile))
            {
                tile.Modulate = fogColor;
            }
        }

        /// <summary>
        /// Helper method to clear all fog of war on main thread.
        /// Resets all tile modulation to fully visible.
        /// </summary>
        private void ClearFogOfWarDeferred()
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile != null)
                {
                    tile.Modulate = FogVisible; // White (no modulation)
                }
            }
        }

        /// <summary>
        /// Converts visibility level to appropriate fog color.
        /// Uses modulation colors that preserve terrain appearance while indicating visibility.
        /// </summary>
        private Color GetFogColorFromVisibility(Darklands.Core.Domain.Vision.VisibilityLevel visibilityLevel)
        {
            return visibilityLevel switch
            {
                Darklands.Core.Domain.Vision.VisibilityLevel.Unseen => FogUnseen,       // Nearly black
                Darklands.Core.Domain.Vision.VisibilityLevel.Explored => FogExplored,   // Gray
                Darklands.Core.Domain.Vision.VisibilityLevel.Visible => FogVisible,     // No modulation
                _ => FogUnseen // Default to unseen for safety
            };
        }

        // Helper methods for strategic grid creation
        private static Darklands.Core.Domain.Grid.Grid PlaceWall(Darklands.Core.Domain.Grid.Grid grid, int x, int y)
        {
            var position = new Darklands.Core.Domain.Grid.Position(x, y);
            return grid.SetTerrain(position, Darklands.Core.Domain.Grid.TerrainType.Wall)
                .IfFail(grid);
        }

        private static Darklands.Core.Domain.Grid.Grid PlaceOpen(Darklands.Core.Domain.Grid.Grid grid, int x, int y)
        {
            var position = new Darklands.Core.Domain.Grid.Position(x, y);
            return grid.SetTerrain(position, Darklands.Core.Domain.Grid.TerrainType.Open)
                .IfFail(grid);
        }

        private static Darklands.Core.Domain.Grid.Grid PlaceForest(Darklands.Core.Domain.Grid.Grid grid, int x, int y)
        {
            var position = new Darklands.Core.Domain.Grid.Position(x, y);
            return grid.SetTerrain(position, Darklands.Core.Domain.Grid.TerrainType.Forest)
                .IfFail(grid);
        }
    }
}
