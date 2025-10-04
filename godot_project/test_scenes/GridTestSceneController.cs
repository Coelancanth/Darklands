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

    // Grid cells: [x, y] = ColorRect node (LEGACY - will be removed after TileMapLayer works)
    private readonly ColorRect[,] _gridCells = new ColorRect[GridSize, GridSize];
    private readonly ColorRect[,] _fovCells = new ColorRect[GridSize, GridSize];
    private readonly ColorRect[,] _actorCells = new ColorRect[GridSize, GridSize]; // Actor overlay above fog

    // Fog of War state: [x, y] = has this cell been explored?
    private readonly bool[,] _exploredCells = new bool[GridSize, GridSize];

    // VS_006 Phase 4: Path visualization
    private readonly List<ColorRect> _pathOverlayNodes = new();

    // Colors for terrain
    private static readonly Color WallColor = Colors.Black;
    private static readonly Color FloorColor = Colors.White;
    private static readonly Color SmokeColor = new Color(0f, 0.6f, 0f); // Green (bushes)
    private static readonly Color PlayerColor = Colors.Blue; // Changed from green
    private static readonly Color DummyColor = Colors.Red;
    private static readonly Color FOVColor = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
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
    /// Creates ColorRect nodes for each grid cell (terrain + FOV + actor layers).
    /// Layer ordering: Terrain (Z=0) → FOV (Z=10) → Actors (Z=20) → Path Preview (Z=15 in ShowPathPreview)
    /// </summary>
    private void CreateGridCells()
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                // Terrain layer (bottom, Z=0) - VS_019: TileMapLayer renders terrain, keep transparent
                var terrainCell = new ColorRect
                {
                    Position = new Vector2(x * CellSize, y * CellSize),
                    Size = new Vector2(CellSize, CellSize),
                    Color = Colors.Transparent, // TileMapLayer renders actual terrain
                    MouseFilter = Control.MouseFilterEnum.Stop // VS_006: Capture mouse input
                };
                AddChild(terrainCell);
                _gridCells[x, y] = terrainCell;

                // FOV overlay layer (middle, Z=10) - starts as unexplored (black fog)
                var fovCell = new ColorRect
                {
                    Position = new Vector2(x * CellSize, y * CellSize),
                    Size = new Vector2(CellSize, CellSize),
                    Color = UnexploredFog, // Start with unexplored fog
                    ZIndex = 10, // Above terrain
                    MouseFilter = Control.MouseFilterEnum.Ignore // VS_006: Let clicks pass through to terrain
                };
                AddChild(fovCell);
                _fovCells[x, y] = fovCell;

                // Actor overlay layer (top, Z=20) - starts transparent
                var actorCell = new ColorRect
                {
                    Position = new Vector2(x * CellSize, y * CellSize),
                    Size = new Vector2(CellSize, CellSize),
                    Color = Colors.Transparent, // Transparent by default
                    ZIndex = 20, // Above FOV (fog of war doesn't hide actors)
                    MouseFilter = Control.MouseFilterEnum.Ignore // Let clicks pass through
                };
                AddChild(actorCell);
                _actorCells[x, y] = actorCell;

                // Mark all cells as unexplored initially
                _exploredCells[x, y] = false;
            }
        }

        _logger.LogInformation("Created {GridSize}x{GridSize} grid cells (3 layers: terrain, FOV, actors)", GridSize, GridSize);
    }

    /// <summary>
    /// Renders a terrain to the TileMapLayer (VS_019 Phase 3).
    /// Gets terrain definition from repository and sets the tile with atlas coordinates.
    /// Uses direct SetCell for all terrains (autotiling deferred - manual tile selection works).
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

        // Direct tile placement (autotiling requires batch SetCellsTerrainConnect - defer to future work)
        _terrainLayer.SetCell(cellPos, sourceId: 4, atlasCoords);
    }

    private async void InitializeGameState()
    {
        // Create actor IDs
        _playerId = ActorId.NewId();
        _dummyId = ActorId.NewId();
        _activeActorId = _playerId; // Start with player's FOV

        // Initialize test terrain: Walls around edges, smoke patches
        // VS_019 Phase 3: Render to TileMapLayer after Core commands

        // FIRST: Fill entire grid with floor tiles (default terrain)
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                await _mediator.Send(new SetTerrainCommand(new Position(x, y), "floor"));
                RenderTerrainToTileMap(new Position(x, y), "floor");
            }
        }

        // THEN: Overlay walls around edges
        for (int x = 0; x < GridSize; x++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(x, 0), "wall_stone"));
            RenderTerrainToTileMap(new Position(x, 0), "wall_stone");

            await _mediator.Send(new SetTerrainCommand(new Position(x, GridSize - 1), "wall_stone"));
            RenderTerrainToTileMap(new Position(x, GridSize - 1), "wall_stone");
        }

        for (int y = 0; y < GridSize; y++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(0, y), "wall_stone"));
            RenderTerrainToTileMap(new Position(0, y), "wall_stone");

            await _mediator.Send(new SetTerrainCommand(new Position(GridSize - 1, y), "wall_stone"));
            RenderTerrainToTileMap(new Position(GridSize - 1, y), "wall_stone");
        }

        // Add some grass patches for testing vision blocking
        await _mediator.Send(new SetTerrainCommand(new Position(10, 10), "grass"));
        RenderTerrainToTileMap(new Position(10, 10), "grass");

        await _mediator.Send(new SetTerrainCommand(new Position(10, 11), "grass"));
        RenderTerrainToTileMap(new Position(10, 11), "grass");

        await _mediator.Send(new SetTerrainCommand(new Position(11, 10), "grass"));
        RenderTerrainToTileMap(new Position(11, 10), "grass");

        // VS_019 Phase 3: Replace interior walls with tree terrain
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

        // VS_019: TileMapLayer renders terrain, ColorRect stays transparent
        // Terrain is already rendered via TileMapLayer, fog overlay handles visibility

        // Set initial actor colors
        SetCellColor(playerStartPos.X, playerStartPos.Y, PlayerColor);
        SetCellColor(dummyStartPos.X, dummyStartPos.Y, DummyColor);

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
    /// Event handler: Actor moved - update cell colors.
    /// Event contains complete information (old + new positions) - no state tracking needed!
    /// </summary>
    private async void OnActorMoved(ActorMovedEvent evt)
    {
        // Restore old cell to terrain color (event tells us the old position!)
        if (evt.OldPosition.X != evt.NewPosition.X || evt.OldPosition.Y != evt.NewPosition.Y)
        {
            // Check if another actor is still at the old position
            await RestoreCellColor(evt.OldPosition);
        }

        // Set new cell to actor color
        var actorColor = evt.ActorId.Equals(_playerId) ? PlayerColor : DummyColor;
        SetCellColor(evt.NewPosition.X, evt.NewPosition.Y, actorColor);

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

                    // VS_019: TileMapLayer renders terrain, no need to paint ColorRect
                    // Keep ColorRect transparent (TileMapLayer visible)
                    _gridCells[x, y].Color = Colors.Transparent;

                    // Show actors ONLY in currently visible areas (real-time)
                    UpdateActorVisibility(pos, playerPosResult, dummyPosResult, true);
                }
                else if (_exploredCells[x, y])
                {
                    // Previously explored but not currently visible: Dim fog
                    _fovCells[x, y].Color = ExploredFog;

                    // VS_019: Keep ColorRect transparent (TileMapLayer + fog overlay visible)
                    _gridCells[x, y].Color = Colors.Transparent;

                    // HIDE actors in explored areas (they may have moved - no memory of enemies)
                    UpdateActorVisibility(pos, playerPosResult, dummyPosResult, false);
                }
                else
                {
                    // Never explored: Opaque black fog (hide terrain completely)
                    _fovCells[x, y].Color = UnexploredFog; // Opaque black overlay
                    _gridCells[x, y].Color = Colors.Transparent; // Don't double-paint black

                    // HIDE actors in unexplored areas (true fog of war)
                    UpdateActorVisibility(pos, playerPosResult, dummyPosResult, false);
                }
            }
        }

        _logger.LogDebug("FOV updated: {VisibleCount} positions visible", evt.VisiblePositions.Count);
    }

    /// <summary>
    /// Updates actor visibility based on exploration state.
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
            _actorCells[pos.X, pos.Y].Color = shouldBeVisible ? PlayerColor : Colors.Transparent;
            return;
        }

        // Check if dummy is at this position
        if (dummyPosResult.IsSuccess && dummyPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = shouldBeVisible ? DummyColor : Colors.Transparent;
            return;
        }

        // No actor here
        _actorCells[pos.X, pos.Y].Color = Colors.Transparent;
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
    /// Sets a grid cell to a specific actor color (on actor overlay layer, above fog).
    /// </summary>
    private void SetCellColor(int x, int y, Color color)
    {
        if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
        {
            _actorCells[x, y].Color = color; // Paint on actor layer (Z=20) not terrain
        }
    }

    /// <summary>
    /// Restores a cell's actor layer to transparent, or keeps actor color if another actor is there.
    /// </summary>
    private async Task RestoreCellColor(Position pos)
    {
        if (pos.X < 0 || pos.X >= GridSize || pos.Y < 0 || pos.Y >= GridSize) return;

        // Check if player is at this position
        var playerPosResult = await _mediator.Send(new GetActorPositionQuery(_playerId));
        if (playerPosResult.IsSuccess && playerPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = PlayerColor;
            return;
        }

        // Check if dummy is at this position
        var dummyPosResult = await _mediator.Send(new GetActorPositionQuery(_dummyId));
        if (dummyPosResult.IsSuccess && dummyPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = DummyColor;
            return;
        }

        // No actor here, make actor layer transparent
        _actorCells[pos.X, pos.Y].Color = Colors.Transparent;
    }

    /// <summary>
    /// Restores a cell to its terrain color when revealed by FOV.
    /// </summary>
    private void RestoreTerrainColor(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return;

        // Determine terrain color based on position
        Color color;

        if (x == 0 || x == GridSize - 1 || y == 0 || y == GridSize - 1)
        {
            color = WallColor;
        }
        else if ((x == 10 && y == 10) || (x == 10 && y == 11) || (x == 11 && y == 10))
        {
            color = SmokeColor;
        }
        else if (y == 15 && x >= 5 && x < 10)
        {
            color = WallColor;
        }
        else
        {
            color = FloorColor;
        }

        _gridCells[x, y].Color = color;
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
