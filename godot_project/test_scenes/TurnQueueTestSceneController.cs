using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Application;
using Darklands.Core.Features.Combat.Application.Queries;
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
/// Controller for VS_007 Turn Queue Test Scene.
/// Tests combat mode detection, enemy auto-scheduling, and movement cancellation.
/// </summary>
/// <remarks>
/// Per ADR-002: ServiceLocator used ONLY in _Ready() to bridge Godot instantiation to DI.
/// Per ADR-004: Subscribes to events as terminal subscriber (no cascading commands/events).
/// VS_007 Test Scenario:
/// - Player starts at (5,5)
/// - Enemy (Goblin) placed at (15,15) - out of initial FOV
/// - Enemy (Orc) placed at (20,20) - out of initial FOV (tests reinforcements)
/// - Click toward enemy → auto-movement starts → enemy appears in FOV → combat mode → movement stops
/// - During combat, new enemy appears → auto-schedules (reinforcement test)
/// </remarks>
public partial class TurnQueueTestSceneController : Node2D
{
    private IMediator _mediator = null!;
    private IGodotEventBus _eventBus = null!;
    private IPathfindingService _pathfindingService = null!;
    private ILogger<TurnQueueTestSceneController> _logger = null!;

    private ActorId _playerId;
    private ActorId _goblinId; // VS_007: Enemy actor for combat mode testing
    private ActorId _orcId; // VS_007: Second enemy for reinforcement testing
    private ActorId _activeActorId; // Whose FOV is currently displayed (toggle with Tab)

    // Movement state (VS_006 Phase 4)
    private CancellationTokenSource? _movementCancellation;
    private Task? _activeMovementTask; // Track active movement task for proper cancellation
    private Position? _lastHoveredPosition; // Track last hovered position for path preview

    private const int GridSize = 30;
    private const int CellSize = 48; // 48x48 pixels per cell

    // Grid cells: [x, y] = ColorRect node
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
    private static readonly Color PlayerColor = Colors.Blue;
    private static readonly Color GoblinColor = Colors.Red; // VS_007: Enemy actor
    private static readonly Color OrcColor = new Color(0.8f, 0.2f, 0f); // VS_007: Second enemy (dark red/orange)
    private static readonly Color FOVColor = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
    private static readonly Color PathPreviewColor = new Color(1f, 0.65f, 0f, 0.6f); // Semi-transparent orange (VS_006)

    // Fog of War overlay colors
    private static readonly Color UnexploredFog = new Color(0, 0, 0, 0.9f); // Nearly opaque black
    private static readonly Color ExploredFog = new Color(0, 0, 0, 0.6f); // Semi-transparent black
    private static readonly Color VisibleFog = new Color(0, 0, 0, 0); // Transparent (no fog)

    public override void _Ready()
    {
        // ADR-002: ServiceLocator ONLY in _Ready() to bridge Godot → DI
        _mediator = ServiceLocator.Get<IMediator>();
        _eventBus = ServiceLocator.Get<IGodotEventBus>();
        _pathfindingService = ServiceLocator.Get<IPathfindingService>(); // VS_006 Phase 4
        _logger = ServiceLocator.Get<ILogger<TurnQueueTestSceneController>>(); // ADR-001: Use ILogger<T>, not GD.Print

        // Create grid visualization
        CreateGridCells();

        // Subscribe to events (ADR-004: Terminal subscriber)
        _eventBus.Subscribe<ActorMovedEvent>(this, OnActorMoved);
        _eventBus.Subscribe<FOVCalculatedEvent>(this, OnFOVCalculated);

        // Initialize game state (async - will complete initialization)
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
                // Terrain layer (bottom, Z=0) - starts as pure black (unexplored)
                var terrainCell = new ColorRect
                {
                    Position = new Vector2(x * CellSize, y * CellSize),
                    Size = new Vector2(CellSize, CellSize),
                    Color = Colors.Black, // Unexplored = pure black
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

    private async void InitializeGameState()
    {
        // Create actor IDs
        _playerId = ActorId.NewId();
        _goblinId = ActorId.NewId(); // VS_007: Enemy actor
        _orcId = ActorId.NewId(); // VS_007: Second enemy
        _activeActorId = _playerId; // Start with player's FOV

        // VS_007 Phase 4: Initialize PlayerContext and TurnQueueRepository
        var playerContext = ServiceLocator.Get<IPlayerContext>();
        playerContext.SetPlayerId(_playerId);

        var turnQueueRepo = ServiceLocator.Get<ITurnQueueRepository>();
        (turnQueueRepo as Darklands.Core.Features.Combat.Infrastructure.InMemoryTurnQueueRepository)?
            .InitializeWithPlayer(_playerId);

        _logger.LogInformation("VS_007: Player context and turn queue initialized with player {PlayerId}", _playerId);

        // Initialize test terrain: Walls around edges, smoke patches
        for (int x = 0; x < GridSize; x++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(x, 0), "wall"));
            await _mediator.Send(new SetTerrainCommand(new Position(x, GridSize - 1), "wall"));
        }

        for (int y = 0; y < GridSize; y++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(0, y), "wall"));
            await _mediator.Send(new SetTerrainCommand(new Position(GridSize - 1, y), "wall"));
        }

        // Add some smoke patches for testing vision blocking
        await _mediator.Send(new SetTerrainCommand(new Position(10, 10), "smoke"));
        await _mediator.Send(new SetTerrainCommand(new Position(10, 11), "smoke"));
        await _mediator.Send(new SetTerrainCommand(new Position(11, 10), "smoke"));

        // Add some interior walls
        for (int x = 5; x < 10; x++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(x, 15), "wall"));
        }

        // VS_007: Register actors at starting positions
        var playerStartPos = new Position(5, 5);
        var goblinStartPos = new Position(15, 15); // Out of initial FOV (range ~10 tiles)
        var orcStartPos = new Position(20, 20); // Further away (reinforcement test)

        await _mediator.Send(new RegisterActorCommand(_playerId, playerStartPos));
        await _mediator.Send(new RegisterActorCommand(_goblinId, goblinStartPos));
        await _mediator.Send(new RegisterActorCommand(_orcId, orcStartPos));

        // DON'T render terrain - it will be revealed through FOV exploration
        // Terrain stays pure black until explored

        // Set initial actor colors (enemies hidden until FOV reveals them)
        SetCellColor(playerStartPos.X, playerStartPos.Y, PlayerColor);
        SetCellColor(goblinStartPos.X, goblinStartPos.Y, GoblinColor); // Will be hidden by fog
        SetCellColor(orcStartPos.X, orcStartPos.Y, OrcColor); // Will be hidden by fog

        // Calculate initial FOV for player (this will reveal starting area)
        await _mediator.Send(new MoveActorCommand(_playerId, playerStartPos));

        _logger.LogInformation("=== VS_007 Turn Queue Test Scene ===");
        _logger.LogInformation("Controls:");
        _logger.LogInformation("  Left Click: Move player (auto-path)");
        _logger.LogInformation("  Right Click: Cancel movement");
        _logger.LogInformation("Test Scenario:");
        _logger.LogInformation("  - Goblin at (15,15) - out of initial FOV");
        _logger.LogInformation("  - Orc at (20,20) - tests reinforcements");
        _logger.LogInformation("  - Click toward Goblin → movement should stop when enemy appears in FOV");
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

        // VS_007: Arrow keys for single-step movement (for testing combat mode)
        ActorId? actorToMove = null;
        Position? direction = null;

        switch (keyEvent.Keycode)
        {
            // Player controls (Arrow keys) - single step movement
            case Key.Right: actorToMove = _playerId; direction = new Position(1, 0); break;
            case Key.Left: actorToMove = _playerId; direction = new Position(-1, 0); break;
            case Key.Down: actorToMove = _playerId; direction = new Position(0, 1); break;
            case Key.Up: actorToMove = _playerId; direction = new Position(0, -1); break;
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
        var actorColor = GetActorColor(evt.ActorId);
        SetCellColor(evt.NewPosition.X, evt.NewPosition.Y, actorColor);

        _logger.LogDebug("{ActorName} moved from ({OldX},{OldY}) to ({NewX},{NewY})",
            GetActorDebugName(evt.ActorId), evt.OldPosition.X, evt.OldPosition.Y, evt.NewPosition.X, evt.NewPosition.Y);
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

        // VS_007: Get actor positions for visibility checking
        var playerPosResult = await _mediator.Send(new GetActorPositionQuery(_playerId));
        var goblinPosResult = await _mediator.Send(new GetActorPositionQuery(_goblinId));
        var orcPosResult = await _mediator.Send(new GetActorPositionQuery(_orcId));

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

                    // Reveal terrain when first explored
                    if (_gridCells[x, y].Color == Colors.Black)
                    {
                        RestoreTerrainColor(x, y); // Paint actual terrain color
                    }

                    // Show actors ONLY in currently visible areas (real-time)
                    UpdateActorVisibility(pos, playerPosResult, goblinPosResult, orcPosResult, true);
                }
                else if (_exploredCells[x, y])
                {
                    // Previously explored but not currently visible: Dim fog
                    _fovCells[x, y].Color = ExploredFog;

                    // HIDE actors in explored areas (they may have moved - no memory of enemies)
                    UpdateActorVisibility(pos, playerPosResult, goblinPosResult, orcPosResult, false);
                }
                else
                {
                    // Never explored: Pure black (hide terrain completely)
                    _fovCells[x, y].Color = Colors.Transparent; // No fog overlay needed
                    _gridCells[x, y].Color = Colors.Black; // Terrain layer is pure black

                    // HIDE actors in unexplored areas (true fog of war)
                    UpdateActorVisibility(pos, playerPosResult, goblinPosResult, orcPosResult, false);
                }
            }
        }

        _logger.LogDebug("FOV updated: {VisibleCount} positions visible", evt.VisiblePositions.Count);
    }

    /// <summary>
    /// Updates actor visibility based on exploration state.
    /// VS_007: Updated for Goblin/Orc instead of Dummy.
    /// </summary>
    private void UpdateActorVisibility(
        Position pos,
        Result<Position> playerPosResult,
        Result<Position> goblinPosResult,
        Result<Position> orcPosResult,
        bool shouldBeVisible)
    {
        // Check if player is at this position
        if (playerPosResult.IsSuccess && playerPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = shouldBeVisible ? PlayerColor : Colors.Transparent;
            return;
        }

        // Check if goblin is at this position
        if (goblinPosResult.IsSuccess && goblinPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = shouldBeVisible ? GoblinColor : Colors.Transparent;
            return;
        }

        // Check if orc is at this position
        if (orcPosResult.IsSuccess && orcPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = shouldBeVisible ? OrcColor : Colors.Transparent;
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
    /// VS_007: Updated for Goblin/Orc instead of Dummy.
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

        // Check if goblin is at this position
        var goblinPosResult = await _mediator.Send(new GetActorPositionQuery(_goblinId));
        if (goblinPosResult.IsSuccess && goblinPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = GoblinColor;
            return;
        }

        // Check if orc is at this position
        var orcPosResult = await _mediator.Send(new GetActorPositionQuery(_orcId));
        if (orcPosResult.IsSuccess && orcPosResult.Value.Equals(pos))
        {
            _actorCells[pos.X, pos.Y].Color = OrcColor;
            return;
        }

        // No actor here, make actor layer transparent
        _actorCells[pos.X, pos.Y].Color = Colors.Transparent;
    }

    /// <summary>
    /// VS_007: Helper to get actor color based on ActorId.
    /// </summary>
    private Color GetActorColor(ActorId actorId)
    {
        if (actorId.Equals(_playerId)) return PlayerColor;
        if (actorId.Equals(_goblinId)) return GoblinColor;
        if (actorId.Equals(_orcId)) return OrcColor;
        return Colors.White; // Fallback for unknown actors
    }

    /// <summary>
    /// Helper to enrich ActorId with readable name for logging.
    /// Format: "ActorType (short-id)" - e.g., "Player (4f26ce80)" or "Goblin (3e56c588)"
    /// </summary>
    /// <remarks>
    /// TEST-ONLY ENRICHMENT: This is a quick fix for test scene logging.
    /// TODO (VS_020): Replace with IActorNameResolver service when Actor entities exist.
    /// Production code should use Actor.NameKey from templates, not hardcoded test names.
    /// </remarks>
    private string GetActorDebugName(ActorId actorId)
    {
        var shortId = actorId.Value.ToString().Substring(0, 8);

        if (actorId.Equals(_playerId)) return $"Player ({shortId})";
        if (actorId.Equals(_goblinId)) return $"Goblin ({shortId})";
        if (actorId.Equals(_orcId)) return $"Orc ({shortId})";

        return $"Unknown ({shortId})"; // Fallback for unregistered actors
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
    /// VS_007 Phase 4: Routes to single-step or auto-path based on combat mode.
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

        // VS_007 Phase 4: Check combat mode BEFORE pathfinding
        var isInCombatQuery = new IsInCombatQuery();
        var isInCombatResult = await _mediator.Send(isInCombatQuery);
        bool isInCombat = isInCombatResult.IsSuccess && isInCombatResult.Value;

        if (isInCombat)
        {
            // COMBAT MODE: Single-step movement toward target (tactical)
            _logger.LogInformation("Combat mode active - single-step movement toward ({TargetX}, {TargetY})", target.X, target.Y);

            // Calculate single step toward target (A* pathfinding)
            var pathResult = _pathfindingService.FindPath(currentPos, target, pos => IsPassable(pos), pos => 1);
            if (pathResult.IsFailure || pathResult.Value.Count == 0)
            {
                _logger.LogDebug("No path to target in combat mode");
                return;
            }

            // Path includes current position at [0], so take [1] for next step
            // If path.Count == 1, we're already at target (shouldn't happen, checked above)
            if (pathResult.Value.Count < 2)
            {
                _logger.LogDebug("Already at target position in combat mode");
                return;
            }

            var nextStep = pathResult.Value[1]; // Skip current position
            _logger.LogInformation("Moving to ({NextX}, {NextY}) [1 step]", nextStep.X, nextStep.Y);

            var moveResult = await _mediator.Send(new MoveActorCommand(actorId, nextStep));
            if (moveResult.IsFailure)
            {
                _logger.LogError("Combat move failed: {Error}", moveResult.Error);
            }
            return;
        }

        // EXPLORATION MODE: Auto-path movement (existing VS_006 behavior)
        _logger.LogDebug("🚶 Exploration mode - auto-path to ({TargetX}, {TargetY})", target.X, target.Y);

        // Find path using A* pathfinding
        var fullPathResult = _pathfindingService.FindPath(
            currentPos,
            target,
            pos => IsPassable(pos),
            pos => 1); // Uniform cost

        if (fullPathResult.IsFailure)
        {
            _logger.LogDebug("No path to ({TargetX}, {TargetY}): {Error}", target.X, target.Y, fullPathResult.Error);
            return;
        }

        var path = fullPathResult.Value;
        _logger.LogInformation("Found path with {PathLength} steps to ({TargetX}, {TargetY})", path.Count, target.X, target.Y);

        // Debug: Log full path for verification
        var pathString = string.Join(" → ", path.Select(p => $"({p.X},{p.Y})"));
        _logger.LogDebug("Path: {PathString}", pathString);

        // Show path preview for the movement (stays visible during execution)
        ShowPathPreview(path);

        // Create cancellation token for this movement
        _movementCancellation = new CancellationTokenSource();

        // Execute movement along path and track the task
        _activeMovementTask = ExecuteMovementAsync(actorId, path, target);
        await _activeMovementTask;

        // Clear path preview after movement completes
        ClearPathPreview();

        // Clean up (only if not already cancelled)
        // NOTE: CancelMovementAsync already handles disposal, so check before disposing
        if (_movementCancellation != null)
        {
            _movementCancellation.Dispose();
            _movementCancellation = null;
        }
        _activeMovementTask = null;
    }

    /// <summary>
    /// Execute movement along path (separated for proper task tracking).
    /// </summary>
    private async Task ExecuteMovementAsync(ActorId actorId, IReadOnlyList<Position> path, Position target)
    {
        var moveResult = await _mediator.Send(
            new MoveAlongPathCommand(actorId, path),
            _movementCancellation!.Token);

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
        // Cache references before await to prevent race conditions
        // (another continuation could null these out during the await)
        var cancellationToDispose = _movementCancellation;
        var taskToAwait = _activeMovementTask;

        if (cancellationToDispose != null && taskToAwait != null)
        {
            _logger.LogInformation("Movement cancelled!");
            cancellationToDispose.Cancel();

            // CRITICAL: Wait for the movement task to complete cancellation
            // This prevents race conditions where new movement starts before old one finishes
            try
            {
                await taskToAwait;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs - suppress
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during movement cancellation");
            }

            // Dispose our cached reference (safe even if field was already nulled)
            cancellationToDispose.Dispose();

            // Only null out fields if they still point to what we cancelled
            // (defensive: another operation might have started meanwhile)
            if (_movementCancellation == cancellationToDispose)
            {
                _movementCancellation = null;
            }
            if (_activeMovementTask == taskToAwait)
            {
                _activeMovementTask = null;
            }

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
