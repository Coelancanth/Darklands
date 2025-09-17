using Godot;
using System;
using System.Threading.Tasks;
using LanguageExt;
using Darklands.Domain.Grid;
using Darklands.Presentation.Views;
using Darklands.Presentation.Presenters;

namespace Darklands.Views
{
    /// <summary>
    /// Godot implementation of IPathVisualizationView for displaying pathfinding results.
    /// Manages visual path overlays, endpoint highlighting, and movement preview graphics.
    /// Uses Node2D containers to organize path elements and provide efficient rendering.
    /// </summary>
    public partial class PathOverlay : Node2D, IPathVisualizationView
    {
        private Node2D _pathContainer = null!;
        private Node2D _endpointContainer = null!;

        // Temporary storage for deferred operations (CallDeferred requires Variant types)
        private Position[] _pendingPath = Array.Empty<Position>();
        private Position[] _pendingEndpoints = Array.Empty<Position>();

        // Simple test interaction (temporary for VS_014 validation)
        private Position? _startPosition = null;
        private IPathVisualizationPresenter? _presenter = null;

        // Visual constants for path display
        private const float TILE_SIZE = 64.0f;
        private const float PATH_LINE_WIDTH = 3.0f;
        private const float ENDPOINT_RADIUS = 8.0f;

        // Colors for path visualization
        private readonly Color PATH_COLOR = Colors.Yellow;
        private readonly Color START_COLOR = Colors.Green;
        private readonly Color END_COLOR = Colors.Red;
        private readonly Color ENDPOINT_COLOR = Colors.LightBlue;
        private readonly Color NO_PATH_COLOR = Colors.Red;

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Initializes child node references for path visualization.
        /// </summary>
        public override void _Ready()
        {
            _pathContainer = GetNode<Node2D>("PathContainer");
            _endpointContainer = GetNode<Node2D>("EndpointContainer");

            if (_pathContainer == null)
                GD.PrintErr("PathOverlay: PathContainer node not found!");

            if (_endpointContainer == null)
                GD.PrintErr("PathOverlay: EndpointContainer node not found!");

            GD.Print("PathOverlay: Ready - Click on grid to test pathfinding!");
        }

        /// <summary>
        /// Sets the presenter for test interactions (called by GameManager).
        /// </summary>
        public void SetPresenter(IPathVisualizationPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>
        /// Simple test input handler - click to set start/end positions for pathfinding.
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (_presenter == null) return;

                // Convert mouse position to grid position
                // CRITICAL FIX: Use GetLocalMousePosition() for correct coordinate conversion
                // mouseEvent.Position is in screen space, we need local space relative to this Node2D
                var localMousePos = GetLocalMousePosition();
                var gridPos = WorldToPosition(localMousePos);

                // Validate grid position is within bounds (30x20 grid)
                if (gridPos.X < 0 || gridPos.X >= 30 || gridPos.Y < 0 || gridPos.Y >= 20)
                {
                    GD.PrintErr($"PathOverlay: Click outside grid bounds: {gridPos}");
                    return;
                }

                if (_startPosition == null)
                {
                    // First click - set start position
                    _startPosition = gridPos;
                    GD.Print($"PathOverlay: Start position set to {gridPos}");
                    _presenter.ClearPathAsync();
                }
                else
                {
                    // Second click - calculate path to end position
                    GD.Print($"PathOverlay: Calculating path from {_startPosition} to {gridPos}");
                    _presenter.ShowPathAsync(_startPosition.Value, gridPos);
                    _startPosition = null; // Reset for next test
                }
            }
        }

        /// <summary>
        /// Displays a calculated path on the grid.
        /// Creates visual lines connecting path positions and markers for start/end.
        /// </summary>
        /// <param name="path">The sequence of positions representing the path</param>
        /// <param name="startPosition">The starting position of the path</param>
        /// <param name="endPosition">The ending position of the path</param>
        public async Task ShowPathAsync(Seq<Position> path, Position startPosition, Position endPosition)
        {
            await Task.Run(() =>
            {
                // Store data for deferred execution
                var pathArray = path.ToArray();
                CallDeferred(nameof(ShowPathInternal), pathArray.Length, startPosition.X, startPosition.Y, endPosition.X, endPosition.Y);

                // Store path data in a field for the deferred method to access
                _pendingPath = pathArray;
            });
        }

        /// <summary>
        /// Deferred method to safely update UI from background thread.
        /// Actual implementation of path visualization using Godot drawing.
        /// </summary>
        private void ShowPathInternal(int pathLength, int startX, int startY, int endX, int endY)
        {
            ClearPathInternal();

            var startPosition = new Position(startX, startY);
            var endPosition = new Position(endX, endY);

            if (_pendingPath.Length == 0)
            {
                GD.Print($"PathOverlay: Empty path from {startPosition} to {endPosition}");
                return;
            }

            // Draw path lines
            for (int i = 0; i < _pendingPath.Length - 1; i++)
            {
                var from = PositionToWorld(_pendingPath[i]);
                var to = PositionToWorld(_pendingPath[i + 1]);

                var line = new Line2D();
                line.AddPoint(from);
                line.AddPoint(to);
                line.Width = PATH_LINE_WIDTH;
                line.DefaultColor = PATH_COLOR;
                _pathContainer.AddChild(line);
            }

            // Draw start marker
            CreatePositionMarker(startPosition, START_COLOR, "START");

            // Draw end marker
            CreatePositionMarker(endPosition, END_COLOR, "END");

            // Draw intermediate path markers
            foreach (var position in _pendingPath)
            {
                if (!position.Equals(startPosition) && !position.Equals(endPosition))
                {
                    CreatePathDot(position);
                }
            }

            GD.Print($"PathOverlay: Displayed path with {_pendingPath.Length} positions");
        }

        /// <summary>
        /// Clears the current path display.
        /// Removes all path visualization elements from the grid.
        /// </summary>
        public async Task ClearPathAsync()
        {
            await Task.Run(() =>
            {
                CallDeferred(nameof(ClearPathInternal));
            });
        }

        /// <summary>
        /// Deferred method to safely clear path display from background thread.
        /// </summary>
        private void ClearPathInternal()
        {
            // Remove all path visualization children
            foreach (Node child in _pathContainer.GetChildren())
            {
                child.QueueFree();
            }
        }

        /// <summary>
        /// Shows a "no path found" indicator between two positions.
        /// Displays visual feedback when pathfinding fails.
        /// </summary>
        /// <param name="startPosition">The attempted start position</param>
        /// <param name="endPosition">The attempted end position</param>
        /// <param name="reason">Reason why no path was found</param>
        public async Task ShowNoPathFoundAsync(Position startPosition, Position endPosition, string reason)
        {
            await Task.Run(() =>
            {
                CallDeferred(nameof(ShowNoPathFoundInternal), startPosition.X, startPosition.Y, endPosition.X, endPosition.Y, reason);
            });
        }

        /// <summary>
        /// Deferred method to show no-path-found visual feedback.
        /// </summary>
        private void ShowNoPathFoundInternal(int startX, int startY, int endX, int endY, string reason)
        {
            ClearPathInternal();

            var startPosition = new Position(startX, startY);
            var endPosition = new Position(endX, endY);

            // Draw dashed line to show attempted path
            var from = PositionToWorld(startPosition);
            var to = PositionToWorld(endPosition);

            var line = new Line2D();
            line.AddPoint(from);
            line.AddPoint(to);
            line.Width = PATH_LINE_WIDTH;
            line.DefaultColor = NO_PATH_COLOR;
            _pathContainer.AddChild(line);

            // Create error markers
            CreatePositionMarker(startPosition, NO_PATH_COLOR, "START");
            CreatePositionMarker(endPosition, NO_PATH_COLOR, "BLOCKED");

            GD.PrintErr($"PathOverlay: No path found from {startPosition} to {endPosition}");
            GD.PrintErr($"  Reason: {reason}");
            GD.PrintErr($"  This usually means one of the positions is blocked or invalid");
        }

        /// <summary>
        /// Highlights potential path endpoints for preview.
        /// Shows valid positions where a path can be calculated to.
        /// </summary>
        /// <param name="fromPosition">The starting position for path calculation</param>
        /// <param name="validEndpoints">Positions that can be reached</param>
        public async Task HighlightValidEndpointsAsync(Position fromPosition, Position[] validEndpoints)
        {
            await Task.Run(() =>
            {
                // Store endpoints for deferred execution
                _pendingEndpoints = validEndpoints;
                CallDeferred(nameof(HighlightValidEndpointsInternal), fromPosition.X, fromPosition.Y, validEndpoints.Length);
            });
        }

        /// <summary>
        /// Deferred method to highlight valid endpoints.
        /// </summary>
        private void HighlightValidEndpointsInternal(int fromX, int fromY, int endpointCount)
        {
            ClearEndpointHighlightingInternal();

            var fromPosition = new Position(fromX, fromY);

            // Highlight starting position
            CreatePositionMarker(fromPosition, START_COLOR, "FROM");

            // Highlight all valid endpoints
            foreach (var endpoint in _pendingEndpoints)
            {
                CreateEndpointHighlight(endpoint);
            }

            GD.Print($"PathOverlay: Highlighted {_pendingEndpoints.Length} valid endpoints from {fromPosition}");
        }

        /// <summary>
        /// Clears endpoint highlighting.
        /// Removes all path endpoint visual indicators.
        /// </summary>
        public async Task ClearEndpointHighlightingAsync()
        {
            await Task.Run(() =>
            {
                CallDeferred(nameof(ClearEndpointHighlightingInternal));
            });
        }

        /// <summary>
        /// Deferred method to clear endpoint highlighting.
        /// </summary>
        private void ClearEndpointHighlightingInternal()
        {
            foreach (Node child in _endpointContainer.GetChildren())
            {
                child.QueueFree();
            }
        }

        /// <summary>
        /// Converts a grid position to world coordinates.
        /// </summary>
        private Vector2 PositionToWorld(Position gridPosition)
        {
            return new Vector2(gridPosition.X * TILE_SIZE + TILE_SIZE / 2, gridPosition.Y * TILE_SIZE + TILE_SIZE / 2);
        }

        /// <summary>
        /// Converts world coordinates to grid position.
        /// </summary>
        private Position WorldToPosition(Vector2 worldPosition)
        {
            return new Position((int)(worldPosition.X / TILE_SIZE), (int)(worldPosition.Y / TILE_SIZE));
        }

        /// <summary>
        /// Creates a position marker with text label.
        /// </summary>
        private void CreatePositionMarker(Position position, Color color, string label)
        {
            var worldPos = PositionToWorld(position);

            // Create circle marker
            var marker = new ColorRect();
            marker.Color = color;
            marker.Size = new Vector2(16, 16);
            marker.Position = worldPos - marker.Size / 2;
            _pathContainer.AddChild(marker);

            // Create text label
            var labelNode = new Label();
            labelNode.Text = label;
            labelNode.Position = worldPos + new Vector2(0, -20);
            labelNode.AddThemeColorOverride("font_color", color);
            _pathContainer.AddChild(labelNode);
        }

        /// <summary>
        /// Creates a small dot for path waypoints.
        /// </summary>
        private void CreatePathDot(Position position)
        {
            var worldPos = PositionToWorld(position);

            var dot = new ColorRect();
            dot.Color = PATH_COLOR;
            dot.Size = new Vector2(6, 6);
            dot.Position = worldPos - dot.Size / 2;
            _pathContainer.AddChild(dot);
        }

        /// <summary>
        /// Creates an endpoint highlight circle.
        /// </summary>
        private void CreateEndpointHighlight(Position position)
        {
            var worldPos = PositionToWorld(position);

            var highlight = new ColorRect();
            highlight.Color = ENDPOINT_COLOR with { A = 0.5f }; // Semi-transparent
            highlight.Size = new Vector2(ENDPOINT_RADIUS * 2, ENDPOINT_RADIUS * 2);
            highlight.Position = worldPos - highlight.Size / 2;
            _endpointContainer.AddChild(highlight);
        }
    }
}