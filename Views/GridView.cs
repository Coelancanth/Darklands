using Darklands.Core.Presentation.Views;
using Darklands.Core.Presentation.Presenters;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

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
        private ILogger? _logger;
        private const int TileSize = 64;
        private int _gridWidth;
        private int _gridHeight;
        private Dictionary<Vector2I, ColorRect> _tiles = new();
        private readonly List<Line2D> _gridLines = new();

        // Colors for different terrain types
        private readonly Color GrassColor = new Color(0.3f, 0.7f, 0.2f); // Green
        private readonly Color StoneColor = new Color(0.5f, 0.5f, 0.5f); // Gray
        private readonly Color WaterColor = new Color(0.2f, 0.4f, 0.8f); // Blue
        private readonly Color HighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.7f); // Yellow with transparency
        private readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark gray with transparency
        private const float GridLineWidth = 1.0f;

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
                _logger?.Error(ex, "GridView._Ready error");
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
                _logger?.Error(ex, "GridView._Input error");
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
        public void SetLogger(ILogger logger)
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

            _logger?.Information("Creating {Width}x{Height} grid with lines", width, height);

            // Create a basic grid with grass tiles as default
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tilePosition = new Vector2I(x, y);
                    CreateTile(tilePosition, GrassColor);
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
                            CreateTile(tilePosition, tileColor);
                        },
                        Fail: _ =>
                        {
                            // Use default grass color for invalid positions
                            var tilePosition = new Vector2I(x, y);
                            CreateTile(tilePosition, GrassColor);
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
            _logger?.Information("{Message} at ({X},{Y})", message, position.X, position.Y);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Shows visual feedback for invalid operations or errors.
        /// </summary>
        public async Task ShowErrorFeedbackAsync(Darklands.Core.Domain.Grid.Position position, string errorMessage)
        {
            // For Phase 4, just print to console
            // Future: Show error indicators or red highlights
            _logger?.Warning("{ErrorMessage} at ({X},{Y})", errorMessage, position.X, position.Y);
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

                    _logger?.Information("User clicked tile ({X},{Y})", tileX, tileY);

                    // Notify the presenter about the tile click
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _presenter.HandleTileClickAsync(gridPosition);
                        }
                        catch (Exception ex)
                        {
                            _logger?.Error(ex, "Error handling tile click at ({X},{Y})", tileX, tileY);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "HandleMouseClick error");
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
                Darklands.Core.Domain.Grid.TerrainType.Forest => GrassColor,
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
    }
}
