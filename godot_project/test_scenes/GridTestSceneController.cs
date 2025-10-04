using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Commands;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Domain.Events;
using Darklands.Core.Features.Movement.Application.Commands;
using Darklands.Core.Features.Movement.Application.Queries;
using Darklands.Core.Features.Movement.Application.Services;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Infrastructure.Events;
using Godot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands;

/// <summary>
/// Controller for Grid FOV Test Scene using pure color rectangles.
/// Demonstrates event-driven architecture: Commands → Core → Events → UI updates.
/// </summary>
/// <remarks>
/// Per ADR-002: ServiceLocator used ONLY in _Ready() to bridge Godot instantiation to DI.
/// Per ADR-004: Subscribes to events as terminal subscriber (no cascading commands/events).
/// </remarks>
public partial class GridTestSceneController : Node2D
{
    private IMediator _mediator = null!;
    private IGodotEventBus _eventBus = null!;
    private IPathfindingService _pathfindingService = null!;
    private ILogger<GridTestSceneController> _logger = null!;
    private Darklands.Core.Features.Grid.Application.ITerrainRepository _terrainRepo = null!; // VS_019 Phase 3

    private ActorId _playerId;
    private ActorId _dummyId;
    private ActorId _activeActorId; // Whose FOV is currently displayed (toggle with Tab)

    // Movement state (VS_006 Phase 4)
    private CancellationTokenSource? _movementCancellation;
    private Task? _activeMovementTask; // Track active movement task for proper cancellation
    private Position? _lastHoveredPosition; // Track last hovered position for path preview

    private const int GridSize = 30;
    private const int CellSize = 48; // 48x48 pixels per cell (ColorRect legacy)
    private const int TileSize = 8; // TileSet tile size (VS_019 Phase 3)

    // VS_019 Phase 3: TileMapLayer for terrain rendering
    private TileMapLayer _terrainLayer = null!;

    // VS_019 Phase 4: Sprite2D actors
    private Sprite2D _playerSprite = null!;
    private Sprite2D _dummySprite = null!;

    // VS_019 Phase 4: Only FOV overlay needed (terrain = TileMapLayer, actors = Sprite2D)
    private readonly ColorRect[,] _fovCells = new ColorRect[GridSize, GridSize];

    // Fog of War state: [x, y] = has this cell been explored?
    private readonly bool[,] _exploredCells = new bool[GridSize, GridSize];

    // VS_006 Phase 4: Path visualization
    private readonly List<ColorRect> _pathOverlayNodes = new();

    // VS_019 Phase 4: Path preview color (actor/terrain colors removed, Sprite2D/TileMapLayer handle rendering)
    private static readonly Color PathPreviewColor = new Color(1f, 0.65f, 0f, 0.6f); // Semi-transparent orange (VS_006)

    // Fog of War overlay colors (VS_019: Adjusted for dark floor tiles)
    private static readonly Color UnexploredFog = new Color(0, 0, 0, 1.0f); // Opaque black (unexplored = fully hidden)
    private static readonly Color ExploredFog = new Color(0, 0, 0, 0.4f); // Light fog (can see terrain but dimmed)
    private static readonly Color VisibleFog = new Color(0, 0, 0, 0); // Transparent (fully visible)

    public override void _Ready()
    {
        // ADR-002: ServiceLocator ONLY in _Ready() to bridge Godot → DI
        _mediator = ServiceLocator.Get<IMediator>();
        _eventBus = ServiceLocator.Get<IGodotEventBus>();
        _pathfindingService = ServiceLocator.Get<IPathfindingService>(); // VS_006 Phase 4
        _logger = ServiceLocator.Get<ILogger<GridTestSceneController>>(); // ADR-001: Use ILogger<T>, not GD.Print
        _terrainRepo = ServiceLocator.Get<Darklands.Core.Features.Grid.Application.ITerrainRepository>(); // VS_019 Phase 3

        // Get TileMapLayer from scene (VS_019 Phase 3)
        _terrainLayer = GetNode<TileMapLayer>("TerrainLayer");

        // VS_019 Phase 4: Get Sprite2D actor nodes
        _playerSprite = GetNode<Sprite2D>("Player");
        _dummySprite = GetNode<Sprite2D>("Dummy");

        // VS_019 Phase 3: Verify terrain repository loaded
        var allTerrainsResult = _terrainRepo.GetAll();
        if (allTerrainsResult.IsSuccess)
        {
            _logger.LogDebug("Terrain repository loaded: {Count} terrains available",
                allTerrainsResult.Value.Count);
        }
        else
        {
            _logger.LogError("Terrain repository FAILED: {Error}", allTerrainsResult.Error);
        }

        // Create grid visualization (LEGACY ColorRect - will be removed)
        CreateGridCells();

        // Subscribe to events (ADR-004: Terminal subscriber)
        _eventBus.Subscribe<ActorMovedEvent>(this, OnActorMoved);
        _eventBus.Subscribe<FOVCalculatedEvent>(this, OnFOVCalculated);

        // Initialize game state
        InitializeGameState();

        // VS_006 Phase 4: Log instructions
        _logger.LogInformation("=== VS_006 Movement Controls ===");
        _logger.LogInformation("Arrow keys: Move player (single step)");
        _logger.LogInformation("WASD: Move dummy (single step)");
        _logger.LogInformation("Left Click: Move player to clicked tile (pathfinding)");
        _logger.LogInformation("Right Click: Cancel movement");
        _logger.LogInformation("Tab: Switch FOV view");
    }

    public override void _ExitTree()
    {
        // Clean up event subscriptions
        _eventBus.UnsubscribeAll(this);
    }

    /// <summary>
    /// Creates ColorRect nodes for FOV overlay (VS_019 Phase 4: terrain + actors removed).
    /// Layer ordering: TileMapLayer (Z=5) → FOV (Z=10) → Sprite2D Actors (Z=20) → Path Preview (Z=15)
    /// </summary>
    private void CreateGridCells()
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                // FOV overlay layer (Z=10) - starts as unexplored (black fog)
                var fovCell = new ColorRect
                {
                    Position = new Vector2(x * CellSize, y * CellSize),
                    Size = new Vector2(CellSize, CellSize),
                    Color = UnexploredFog, // Start with unexplored fog
                    ZIndex = 10, // Above TileMapLayer (Z=5), below Sprite2D actors (Z=20)
                    MouseFilter = Control.MouseFilterEnum.Stop // VS_006: Capture mouse input
                };
                AddChild(fovCell);
                _fovCells[x, y] = fovCell;

                // Mark all cells as unexplored initially
                _exploredCells[x, y] = false;
            }
        }

        _logger.LogInformation("Created {GridSize}x{GridSize} FOV overlay grid", GridSize, GridSize);
    }

    /// <summary>
    /// Renders a terrain to the TileMapLayer (VS_019_FOLLOWUP: Direct SetCell for non-autotiled terrains).
    /// For wall_stone, use RenderWallsWithAutotiling batch method instead.
    /// </summary>
    private void RenderTerrainToTileMap(Position pos, string terrainName)
    {
        var terrainResult = _terrainRepo.GetByName(terrainName);
        if (terrainResult.IsFailure)
        {
            _logger.LogError("Failed to render terrain '{TerrainName}' at ({X},{Y}): {Error}",
                terrainName, pos.X, pos.Y, terrainResult.Error);
            return;
        }

        var terrain = terrainResult.Value;
        var cellPos = new Vector2I(pos.X, pos.Y);
        var atlasCoords = new Vector2I(terrain.AtlasX, terrain.AtlasY);

        // Direct tile placement for non-autotiled terrains (floor, grass, trees)
        _terrainLayer.SetCell(cellPos, sourceId: 4, atlasCoords);
    }

    /// <summary>
    /// Renders wall tiles with manual edge/corner assignment (VS_019_FOLLOWUP).
    /// Note: Godot terrain autotiling fails for symmetric bitmasks - both left and right edges
    /// have identical neighbor patterns, causing autotiling to arbitrarily pick one variant.
    /// Manual position-based assignment ensures correct tiles for each edge/corner.
    /// </summary>
    private void RenderWallsWithAutotiling(List<Position> wallPositions)
    {
        if (wallPositions.Count == 0) return;

        _logger.LogDebug("Rendering {WallCount} wall cells with manual edge/corner assignment...", wallPositions.Count);

        foreach (var pos in wallPositions)
        {
            var cellPos = new Vector2I(pos.X, pos.Y);
            Vector2I atlasCoords;

            // Manually determine tile variant based on position
            // Corners
            if (pos.X == 0 && pos.Y == 0)
            {
                atlasCoords = new Vector2I(0, 0); // Top-left corner
            }
            else if (pos.X == GridSize - 1 && pos.Y == 0)
            {
                atlasCoords = new Vector2I(3, 0); // Top-right corner
            }
            else if (pos.X == 0 && pos.Y == GridSize - 1)
            {
                atlasCoords = new Vector2I(0, 2); // Bottom-left corner
            }
            else if (pos.X == GridSize - 1 && pos.Y == GridSize - 1)
            {
                atlasCoords = new Vector2I(3, 2); // Bottom-right corner
            }
            // Edges
            else if (pos.Y == 0)
            {
                atlasCoords = new Vector2I(1, 0); // Top edge
            }
            else if (pos.Y == GridSize - 1)
            {
                atlasCoords = new Vector2I(2, 4); // Bottom edge
            }
            else if (pos.X == 0)
            {
                atlasCoords = new Vector2I(0, 1); // Left edge (wall_middle_left)
            }
            else if (pos.X == GridSize - 1)
            {
                atlasCoords = new Vector2I(3, 1); // Right edge (wall_middle_right)
            }
            else
            {
                // Interior wall (shouldn't happen in our test case, use generic)
                atlasCoords = new Vector2I(0, 0);
            }

            _terrainLayer.SetCell(cellPos, sourceId: 4, atlasCoords);
        }

        _logger.LogDebug("Manual wall tiling complete (4 corners + top/bottom/left/right edges)");
    }

    private async void InitializeGameState()
    {
        // Create actor IDs
        _playerId = ActorId.NewId();
        _dummyId = ActorId.NewId();
        _activeActorId = _playerId; // Start with player's FOV

        // Initialize test terrain: Walls around edges, smoke patches
        // VS_019_FOLLOWUP: Batch wall autotiling - collect positions first, render later

        // FIRST: Fill entire grid with floor tiles (default terrain)
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                await _mediator.Send(new SetTerrainCommand(new Position(x, y), "floor"));
                RenderTerrainToTileMap(new Position(x, y), "floor");
            }
        }

        // SECOND: Register walls in Core AND collect positions for autotiling
        var wallPositions = new List<Position>();

        // Top and bottom edges
        for (int x = 0; x < GridSize; x++)
        {
            var topPos = new Position(x, 0);
            var bottomPos = new Position(x, GridSize - 1);

            await _mediator.Send(new SetTerrainCommand(topPos, "wall_stone"));
            wallPositions.Add(topPos);

            await _mediator.Send(new SetTerrainCommand(bottomPos, "wall_stone"));
            wallPositions.Add(bottomPos);
        }

        // Left and right edges (skip corners - already added)
        for (int y = 1; y < GridSize - 1; y++)
        {
            var leftPos = new Position(0, y);
            var rightPos = new Position(GridSize - 1, y);

            await _mediator.Send(new SetTerrainCommand(leftPos, "wall_stone"));
            wallPositions.Add(leftPos);

            await _mediator.Send(new SetTerrainCommand(rightPos, "wall_stone"));
            wallPositions.Add(rightPos);
        }

        // THIRD: Apply autotiling to ALL walls at once (batch processing for neighbor analysis)
        RenderWallsWithAutotiling(wallPositions);

        // FOURTH: Add grass patches (non-autotiled, direct placement)
        await _mediator.Send(new SetTerrainCommand(new Position(10, 10), "grass"));
        RenderTerrainToTileMap(new Position(10, 10), "grass");

        await _mediator.Send(new SetTerrainCommand(new Position(10, 11), "grass"));
        RenderTerrainToTileMap(new Position(10, 11), "grass");

        await _mediator.Send(new SetTerrainCommand(new Position(11, 10), "grass"));
        RenderTerrainToTileMap(new Position(11, 10), "grass");

        // FIFTH: Add tree terrain (non-autotiled, direct placement)
        for (int x = 5; x < 10; x++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(x, 15), "tree"));
            RenderTerrainToTileMap(new Position(x, 15), "tree");
        }

        // Register actors at starting positions
        var playerStartPos = new Position(5, 5);
        var dummyStartPos = new Position(20, 20);

        await _mediator.Send(new RegisterActorCommand(_playerId, playerStartPos));
        await _mediator.Send(new RegisterActorCommand(_dummyId, dummyStartPos));

        // VS_019 Phase 4: Position Sprite2D actors (already positioned in scene, this is redundant but explicit)
        _playerSprite.Position = GridToPixelCenter(playerStartPos);
        _dummySprite.Position = GridToPixelCenter(dummyStartPos);

        // Calculate initial FOV for player (this will reveal starting area)
        await _mediator.Send(new MoveActorCommand(_playerId, playerStartPos));

        _logger.LogInformation("Grid Test Scene initialized!");
        _logger.LogInformation("Controls: Arrow Keys = Player, WASD = Dummy, Tab = Switch FOV view");
        _logger.LogInformation("Cell size: {CellSize}x{CellSize} pixels, Grid: {GridSize}x{GridSize} cells", CellSize, CellSize, GridSize, GridSize);
    }

    public override void _Input(InputEvent @event)
    {
        // VS_006 Phase 4: Mouse hover for path preview (only when NOT moving)
        if (@event is InputEventMouseMotion motionEvent)
        {
            // Don't show path preview during active movement
            if (_movementCancellation != null)
            {
                return;
            }

            var gridPos = ScreenToGridPosition(motionEvent.Position);
            if (IsValidGridPosition(gridPos) && gridPos != _lastHoveredPosition)
            {
                _lastHoveredPosition = gridPos;
                ShowPathPreviewForHover(_playerId, gridPos);
            }
            return;
        }

        // VS_006 Phase 4: Mouse click to execute movement
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                // Left click: Execute movement along previewed path
                var gridPos = ScreenToGridPosition(mouseEvent.Position);
                _logger.LogDebug("Mouse clicked at screen ({ScreenX}, {ScreenY}) → grid ({GridX}, {GridY})",
                    mouseEvent.Position.X, mouseEvent.Position.Y, gridPos.X, gridPos.Y);

                if (IsValidGridPosition(gridPos))
                {
                    ClickToMove(_playerId, gridPos);
                    GetViewport().SetInputAsHandled();
                }
                else
                {
                    _logger.LogDebug("Click outside grid bounds");
                }
                return;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right)
            {
                // Right click: Cancel active movement
                CancelMovement();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        // Keyboard input handling (existing)
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
            return;

        // Tab: Switch which actor's FOV is displayed
        if (keyEvent.Keycode == Key.Tab)
        {
            _activeActorId = _activeActorId.Equals(_playerId) ? _dummyId : _playerId;
            var actorName = _activeActorId.Equals(_playerId) ? "Player" : "Dummy";
            _logger.LogInformation("Switched FOV view to: {ActorName}", actorName);

            // Trigger FOV refresh for newly active actor
            RefreshFOVDisplay();
            return;
        }

        // Determine which actor and direction based on input
        ActorId? actorToMove = null;
        Position? direction = null;

        switch (keyEvent.Keycode)
        {
            // Player controls (Arrow keys)
            case Key.Right: actorToMove = _playerId; direction = new Position(1, 0); break;
            case Key.Left: actorToMove = _playerId; direction = new Position(-1, 0); break;
            case Key.Down: actorToMove = _playerId; direction = new Position(0, 1); break;
            case Key.Up: actorToMove = _playerId; direction = new Position(0, -1); break;

            // Dummy controls (WASD)
            case Key.D: actorToMove = _dummyId; direction = new Position(1, 0); break;
            case Key.A: actorToMove = _dummyId; direction = new Position(-1, 0); break;
            case Key.S: actorToMove = _dummyId; direction = new Position(0, 1); break;
            case Key.W: actorToMove = _dummyId; direction = new Position(0, -1); break;
        }

        if (actorToMove.HasValue && direction.HasValue)
        {
            TryMoveActor(actorToMove.Value, direction.Value);
        }
    }

    private async void TryMoveActor(ActorId actorId, Position direction)
    {
        // Get current position from Core
        var currentPosResult = await _mediator.Send(new GetActorPositionQuery(actorId));

        if (currentPosResult.IsFailure)
        {
            _logger.LogError("Failed to get actor position: {Error}", currentPosResult.Error);
            return;
        }

        var currentPos = currentPosResult.Value;
        var newPos = new Position(currentPos.X + direction.X, currentPos.Y + direction.Y);

        // Send move command (Core handles validation, FOV calc, events)
        var moveResult = await _mediator.Send(new MoveActorCommand(actorId, newPos));

        if (moveResult.IsFailure)
        {
            _logger.LogDebug("Move blocked: {Error}", moveResult.Error);
        }
        // Success: Events will trigger OnActorMoved + OnFOVCalculated
    }

    /// <summary>
    /// Event handler: Actor moved - tween Sprite2D to new position (VS_019 Phase 4).
    /// </summary>
    private void OnActorMoved(ActorMovedEvent evt)
    {
        // Determine which sprite to move
        var sprite = evt.ActorId.Equals(_playerId) ? _playerSprite : _dummySprite;

        // Create smooth tween animation to new position
        var tween = CreateTween();
        tween.TweenProperty(sprite, "position", GridToPixelCenter(evt.NewPosition), 0.1); // 100ms smooth movement

        _logger.LogDebug("Actor moved from ({OldX},{OldY}) to ({NewX},{NewY})",
            evt.OldPosition.X, evt.OldPosition.Y, evt.NewPosition.X, evt.NewPosition.Y);
    }

    /// <summary>
    /// Event handler: FOV calculated - update fog of war (3-state system).
    /// TRUE FOG OF WAR:
    /// - Unexplored: Pure black (no terrain, no actors visible)
    /// - Explored: Show terrain memory, HIDE actors (they may have moved)
    /// - Visible (FOV): Show terrain AND actors (real-time)
    /// </summary>
    private async void OnFOVCalculated(FOVCalculatedEvent evt)
    {
        if (!evt.ActorId.Equals(_activeActorId))
            return; // Only show active actor's FOV

        // Create set of currently visible positions for fast lookup
        var visibleSet = new HashSet<Position>(evt.VisiblePositions);

        // Get actor positions for visibility checking
        var playerPosResult = await _mediator.Send(new GetActorPositionQuery(_playerId));
        var dummyPosResult = await _mediator.Send(new GetActorPositionQuery(_dummyId));

        // Update fog of war for all cells (3-state system)
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                var pos = new Position(x, y);

                if (visibleSet.Contains(pos))
                {
                    // Currently visible (FOV): No fog, mark as explored
                    _fovCells[x, y].Color = VisibleFog;
                    _exploredCells[x, y] = true;
                }
                else if (_exploredCells[x, y])
                {
                    // Previously explored but not currently visible: Dim fog
                    _fovCells[x, y].Color = ExploredFog;
                }
                else
                {
                    // Never explored: Opaque black fog (hide terrain completely)
                    _fovCells[x, y].Color = UnexploredFog;
                }

                // VS_019 Phase 4: Sprite2D actors render at Z=20 (above fog), visibility handled separately
                UpdateActorVisibility(pos, playerPosResult, dummyPosResult, visibleSet.Contains(pos));
            }
        }

        _logger.LogDebug("FOV updated: {VisibleCount} positions visible", evt.VisiblePositions.Count);
    }

    /// <summary>
    /// Updates actor visibility based on exploration state (VS_019 Phase 4: Sprite2D visibility).
    /// </summary>
    private void UpdateActorVisibility(
        Position pos,
        Result<Position> playerPosResult,
        Result<Position> dummyPosResult,
        bool shouldBeVisible)
    {
        // Check if player is at this position
        if (playerPosResult.IsSuccess && playerPosResult.Value.Equals(pos))
        {
            _playerSprite.Visible = shouldBeVisible;
            return;
        }

        // Check if dummy is at this position
        if (dummyPosResult.IsSuccess && dummyPosResult.Value.Equals(pos))
        {
            _dummySprite.Visible = shouldBeVisible;
            return;
        }
    }

    /// <summary>
    /// Manually refresh FOV display (used when switching active actor with Tab).
    /// </summary>
    private async void RefreshFOVDisplay()
    {
        // Get active actor position
        var posResult = await _mediator.Send(new GetActorPositionQuery(_activeActorId));
        if (posResult.IsFailure) return;

        // Recalculate FOV
        await _mediator.Send(new MoveActorCommand(_activeActorId, posResult.Value));
    }

    /// <summary>
    /// Converts grid position to pixel coordinates (centered in cell) for Sprite2D positioning.
    /// </summary>
    private Vector2 GridToPixelCenter(Position gridPos)
    {
        // Center of cell = (grid * cellSize) + (cellSize / 2)
        return new Vector2(
            gridPos.X * CellSize + CellSize / 2f,
            gridPos.Y * CellSize + CellSize / 2f);
    }

    // ===== VS_006 Phase 4: Click-to-Move Pathfinding =====

    /// <summary>
    /// Converts screen coordinates to grid position.
    /// </summary>
    private Position ScreenToGridPosition(Vector2 screenPos)
    {
        var gridX = (int)(screenPos.X / CellSize);
        var gridY = (int)(screenPos.Y / CellSize);
        return new Position(gridX, gridY);
    }

    /// <summary>
    /// Checks if position is within grid bounds.
    /// </summary>
    private bool IsValidGridPosition(Position pos)
    {
        return pos.X >= 0 && pos.X < GridSize && pos.Y >= 0 && pos.Y < GridSize;
    }

    /// <summary>
    /// Click-to-move: Pathfind to target and execute movement.
    /// </summary>
    private async void ClickToMove(ActorId actorId, Position target)
    {
        // Cancel any active movement first AND wait for it to complete
        await CancelMovementAsync();

        // Get current position
        var currentPosResult = await _mediator.Send(new GetActorPositionQuery(actorId));
        if (currentPosResult.IsFailure)
        {
            _logger.LogError("Failed to get actor position: {Error}", currentPosResult.Error);
            return;
        }

        var currentPos = currentPosResult.Value;

        // If already at target, do nothing
        if (currentPos.Equals(target))
        {
            _logger.LogDebug("Already at target position");
            return;
        }

        // Find path using A* pathfinding
        var pathResult = _pathfindingService.FindPath(
            currentPos,
            target,
            pos => IsPassable(pos),
            pos => 1); // Uniform cost for VS_006

        if (pathResult.IsFailure)
        {
            _logger.LogDebug("No path to ({TargetX}, {TargetY}): {Error}", target.X, target.Y, pathResult.Error);
            return;
        }

        var path = pathResult.Value;
        _logger.LogInformation("Found path with {PathLength} steps to ({TargetX}, {TargetY})", path.Count, target.X, target.Y);

        // Debug: Log full path for verification
        var pathString = string.Join(" → ", path.Select(p => $"({p.X},{p.Y})"));
        _logger.LogDebug("Path: {PathString}", pathString);

        // Path preview already visible from hover - just execute movement
        // Create cancellation token for this movement
        _movementCancellation = new CancellationTokenSource();

        // Execute movement along path and track the task
        _activeMovementTask = ExecuteMovementAsync(actorId, path, target);
        await _activeMovementTask;

        // Clean up
        _activeMovementTask = null;
        _movementCancellation?.Dispose();
        _movementCancellation = null;
    }

    /// <summary>
    /// Execute movement along path (separated for proper task tracking).
    /// </summary>
    private async Task ExecuteMovementAsync(ActorId actorId, IReadOnlyList<Position> path, Position target)
    {
        var moveResult = await _mediator.Send(
            new MoveAlongPathCommand(actorId, path),
            _movementCancellation!.Token);

        // Clear path preview after movement completes/fails/cancels
        ClearPathPreview();

        if (moveResult.IsFailure)
        {
            _logger.LogError("Movement failed: {Error}", moveResult.Error);
        }
        else
        {
            _logger.LogInformation("Movement completed to ({TargetX}, {TargetY})", target.X, target.Y);
        }
    }

    /// <summary>
    /// Cancel active movement (right-click) - ASYNC version for proper awaiting.
    /// </summary>
    private async Task CancelMovementAsync()
    {
        if (_movementCancellation != null && _activeMovementTask != null)
        {
            _logger.LogInformation("Movement cancelled!");
            _movementCancellation.Cancel();

            // CRITICAL: Wait for the movement task to complete cancellation
            // This prevents race conditions where new movement starts before old one finishes
            try
            {
                await _activeMovementTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs - suppress
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during movement cancellation");
            }

            _movementCancellation.Dispose();
            _movementCancellation = null;
            _activeMovementTask = null;

            // Clear path preview when cancelling
            ClearPathPreview();
        }
    }

    /// <summary>
    /// Cancel active movement (right-click) - synchronous wrapper for event handlers.
    /// </summary>
    private async void CancelMovement()
    {
        await CancelMovementAsync();
    }

    /// <summary>
    /// Check if a position is passable (for pathfinding).
    /// </summary>
    private bool IsPassable(Position pos)
    {
        // Bounds check
        if (!IsValidGridPosition(pos)) return false;

        // Check terrain (walls are impassable)
        // Edges are walls
        if (pos.X == 0 || pos.X == GridSize - 1 || pos.Y == 0 || pos.Y == GridSize - 1)
            return false;

        // Horizontal wall at Y=15, X=[5,10)
        if (pos.Y == 15 && pos.X >= 5 && pos.X < 10)
            return false;

        // All other tiles are passable (smoke is passable)
        return true;
    }

    /// <summary>
    /// Show path preview for hover (before click to commit).
    /// </summary>
    private void ShowPathPreviewForHover(ActorId actorId, Position target)
    {
        // CRITICAL: Clear existing preview BEFORE calculating new path
        // Prevents artifact of old path overlaying new path when destination changes
        ClearPathPreview();

        // Get current position (synchronous - use cached position if available)
        var currentPosResult = _mediator.Send(new GetActorPositionQuery(actorId)).Result;
        if (currentPosResult.IsFailure) return;

        var currentPos = currentPosResult.Value;
        if (currentPos.Equals(target))
        {
            return; // No path needed (already cleared above)
        }

        // Calculate path using A* pathfinding
        var pathResult = _pathfindingService.FindPath(
            currentPos,
            target,
            pos => IsPassable(pos),
            pos => 1); // Uniform cost

        if (pathResult.IsFailure)
        {
            return; // No valid path (already cleared above)
        }

        // Show the preview
        ShowPathPreview(pathResult.Value);
    }

    /// <summary>
    /// Show path visualization overlay.
    /// </summary>
    private void ShowPathPreview(IReadOnlyList<Position> path)
    {
        // Clear any existing path overlay
        ClearPathPreview();

        // Create overlay nodes for each position in path
        foreach (var pos in path)
        {
            var pathNode = new ColorRect
            {
                Position = new Vector2(pos.X * CellSize, pos.Y * CellSize),
                Size = new Vector2(CellSize, CellSize),
                Color = PathPreviewColor,
                ZIndex = 15, // Between FOV (10) and actors (20)
                MouseFilter = Control.MouseFilterEnum.Ignore // Let clicks pass through
            };
            AddChild(pathNode);
            _pathOverlayNodes.Add(pathNode);
        }
    }

    /// <summary>
    /// Clear path visualization overlay.
    /// </summary>
    private void ClearPathPreview()
    {
        foreach (var node in _pathOverlayNodes)
        {
            RemoveChild(node);
            node.QueueFree();
        }
        _pathOverlayNodes.Clear();
    }
}
