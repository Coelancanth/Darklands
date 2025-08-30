using Darklands.Core.Presentation.Views;
using Darklands.Core.Presentation.Presenters;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darklands.Views
{
    /// <summary>
    /// Concrete Godot implementation of the grid view interface.
    /// Handles the visual representation of the tactical combat grid using Godot's TileMapLayer.
    /// Manages tile rendering, highlighting, and user input detection.
    /// </summary>
    public partial class GridView : Node2D, IGridView
    {
        private TileMapLayer? _tileMapLayer;
        private GridPresenter? _presenter;
        private const int TileSize = 16;
        private int _gridWidth;
        private int _gridHeight;

        // Tile source IDs for different terrain types and states
        private const int GrassTileId = 0;
        private const int StoneTileId = 1;
        private const int WaterTileId = 2;
        private const int HighlightTileId = 3;

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Sets up the TileMapLayer and initializes the grid display system.
        /// </summary>
        public override void _Ready()
        {
            try
            {
                // Find the TileMapLayer child node
                _tileMapLayer = GetNode<TileMapLayer>("TileMapLayer");
                if (_tileMapLayer == null)
                {
                    GD.PrintErr("GridView: TileMapLayer child node not found!");
                    return;
                }

                GD.Print("GridView initialized successfully");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"GridView._Ready error: {ex.Message}");
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
                GD.PrintErr($"GridView._Input error: {ex.Message}");
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
        /// Displays the grid boundaries with the specified dimensions.
        /// Creates the visual grid and sets up tile rendering.
        /// </summary>
        public async Task DisplayGridBoundariesAsync(int width, int height)
        {
            await CallDeferredAsync(width, height);
            
            async Task CallDeferredAsync(int w, int h)
            {
                if (_tileMapLayer == null) return;
                
                _gridWidth = w;
                _gridHeight = h;
                
                // Clear any existing tiles
                _tileMapLayer.Clear();
                
                // Create a basic grid with grass tiles as default
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        var tilePosition = new Vector2I(x, y);
                        _tileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero, GrassTileId);
                    }
                }
                
                GD.Print($"Grid boundaries displayed: {width}x{height}");
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Updates the entire grid display with the current tile states.
        /// Renders all tiles according to their terrain types and occupancy.
        /// </summary>
        public async Task RefreshGridAsync(Darklands.Core.Domain.Grid.Grid grid)
        {
            await CallDeferredAsync(grid);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.Grid g)
            {
                if (_tileMapLayer == null) return;
                
                _gridWidth = g.Width;
                _gridHeight = g.Height;
                
                // Clear existing tiles
                _tileMapLayer.Clear();
                
                // Render each tile based on its state
                for (int x = 0; x < g.Width; x++)
                {
                    for (int y = 0; y < g.Height; y++)
                    {
                        var position = new Darklands.Core.Domain.Grid.Position(x, y);
                        var tileResult = g.GetTile(position);
                        
                        await tileResult.Match(
                            Succ: async tile =>
                            {
                                var tileId = GetTileIdFromTerrain(tile.TerrainType);
                                var tilePosition = new Vector2I(x, y);
                                _tileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero, tileId);
                                await Task.CompletedTask;
                            },
                            Fail: async _ =>
                            {
                                // Use default grass tile for invalid positions
                                var tilePosition = new Vector2I(x, y);
                                _tileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero, GrassTileId);
                                await Task.CompletedTask;
                            }
                        );
                    }
                }
                
                GD.Print($"Grid refreshed: {g.Width}x{g.Height}");
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Updates a single tile's visual representation.
        /// </summary>
        public async Task UpdateTileAsync(Darklands.Core.Domain.Grid.Position position, Darklands.Core.Domain.Grid.Tile tile)
        {
            await CallDeferredAsync(position, tile);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.Position pos, Darklands.Core.Domain.Grid.Tile t)
            {
                if (_tileMapLayer == null) return;
                
                var tileId = GetTileIdFromTerrain(t.TerrainType);
                var tilePosition = new Vector2I(pos.X, pos.Y);
                _tileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero, tileId);
                
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Highlights a tile to indicate selection, hover, or special state.
        /// </summary>
        public async Task HighlightTileAsync(Darklands.Core.Domain.Grid.Position position, HighlightType highlightType)
        {
            await CallDeferredAsync(position, highlightType);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.Position pos, HighlightType type)
            {
                if (_tileMapLayer == null) return;
                
                // For Phase 4, use a simple highlight overlay
                // Future: Different highlight types could use different visual effects
                var tilePosition = new Vector2I(pos.X, pos.Y);
                _tileMapLayer.SetCell(tilePosition, 1, Vector2I.Zero, HighlightTileId); // Layer 1 for overlays
                
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Removes highlighting from a tile.
        /// </summary>
        public async Task UnhighlightTileAsync(Darklands.Core.Domain.Grid.Position position)
        {
            await CallDeferredAsync(position);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.Position pos)
            {
                if (_tileMapLayer == null) return;
                
                // Remove highlight overlay
                var tilePosition = new Vector2I(pos.X, pos.Y);
                _tileMapLayer.EraseCell(tilePosition); // Remove from overlay layer
                
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Shows visual feedback for successful operations.
        /// </summary>
        public async Task ShowSuccessFeedbackAsync(Darklands.Core.Domain.Grid.Position position, string message)
        {
            await CallDeferredAsync(position, message);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.Position pos, string msg)
            {
                // For Phase 4, just print to console
                // Future: Show floating text or particle effects
                GD.Print($"Success at {pos}: {msg}");
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Shows visual feedback for invalid operations or errors.
        /// </summary>
        public async Task ShowErrorFeedbackAsync(Darklands.Core.Domain.Grid.Position position, string errorMessage)
        {
            await CallDeferredAsync(position, errorMessage);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.Position pos, string msg)
            {
                // For Phase 4, just print to console
                // Future: Show error indicators or red highlights
                GD.PrintErr($"Error at {pos}: {msg}");
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Clears all visual overlays and resets the grid to clean state.
        /// </summary>
        public async Task ClearOverlaysAsync()
        {
            await CallDeferredAsync();
            
            async Task CallDeferredAsync()
            {
                if (_tileMapLayer == null) return;
                
                // Clear overlay layer (layer 1)
                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        var tilePosition = new Vector2I(x, y);
                        _tileMapLayer.EraseCell(tilePosition);
                    }
                }
                
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Handles mouse clicks on the grid by converting screen coordinates to grid positions.
        /// </summary>
        private void HandleMouseClick(Vector2 globalPosition)
        {
            try
            {
                if (_tileMapLayer == null || _presenter == null) return;
                
                // Convert global position to local tile coordinates
                var localPosition = _tileMapLayer.ToLocal(globalPosition);
                var tilePosition = _tileMapLayer.LocalToMap(localPosition);
                
                // Validate that the click is within grid bounds
                if (tilePosition.X >= 0 && tilePosition.X < _gridWidth && 
                    tilePosition.Y >= 0 && tilePosition.Y < _gridHeight)
                {
                    var gridPosition = new Darklands.Core.Domain.Grid.Position(tilePosition.X, tilePosition.Y);
                    
                    // Notify the presenter about the tile click
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _presenter.HandleTileClickAsync(gridPosition);
                        }
                        catch (Exception ex)
                        {
                            GD.PrintErr($"Error handling tile click: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"HandleMouseClick error: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts terrain type to tile ID for rendering.
        /// </summary>
        private int GetTileIdFromTerrain(Darklands.Core.Domain.Grid.TerrainType terrain)
        {
            return terrain switch
            {
                Darklands.Core.Domain.Grid.TerrainType.Open => GrassTileId,
                Darklands.Core.Domain.Grid.TerrainType.Rocky => StoneTileId,
                Darklands.Core.Domain.Grid.TerrainType.Water => WaterTileId,
                Darklands.Core.Domain.Grid.TerrainType.Forest => GrassTileId, // For Phase 4, use grass
                Darklands.Core.Domain.Grid.TerrainType.Hill => StoneTileId,   // Use stone for hills
                Darklands.Core.Domain.Grid.TerrainType.Swamp => WaterTileId,  // Use water for swamp
                Darklands.Core.Domain.Grid.TerrainType.Wall => StoneTileId,   // Use stone for walls
                _ => GrassTileId // Default to grass
            };
        }
    }
}