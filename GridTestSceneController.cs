using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Commands;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Domain.Events;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Infrastructure.Events;
using Godot;
using MediatR;

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

    private ActorId _playerId;
    private ActorId _dummyId;
    private ActorId _activeActorId; // Whose FOV is currently displayed (toggle with Tab)

    private const int GridSize = 30;
    private const int CellSize = 48; // 48x48 pixels per cell

    // Grid cells: [x, y] = ColorRect node
    private readonly ColorRect[,] _gridCells = new ColorRect[GridSize, GridSize];
    private readonly ColorRect[,] _fovCells = new ColorRect[GridSize, GridSize];

    // Colors for terrain
    private static readonly Color WallColor = Colors.Black;
    private static readonly Color FloorColor = Colors.White;
    private static readonly Color SmokeColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray
    private static readonly Color PlayerColor = Colors.Green;
    private static readonly Color DummyColor = Colors.Red;
    private static readonly Color FOVColor = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow

    public override void _Ready()
    {
        // ADR-002: ServiceLocator ONLY in _Ready() to bridge Godot → DI
        _mediator = ServiceLocator.Get<IMediator>();
        _eventBus = ServiceLocator.Get<IGodotEventBus>();

        // Create grid visualization
        CreateGridCells();

        // Subscribe to events (ADR-004: Terminal subscriber)
        _eventBus.Subscribe<ActorMovedEvent>(this, OnActorMoved);
        _eventBus.Subscribe<FOVCalculatedEvent>(this, OnFOVCalculated);

        // Initialize game state
        InitializeGameState();
    }

    public override void _ExitTree()
    {
        // Clean up event subscriptions
        _eventBus.UnsubscribeAll(this);
    }

    /// <summary>
    /// Creates ColorRect nodes for each grid cell (terrain + FOV overlay layers).
    /// </summary>
    private void CreateGridCells()
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                // Terrain layer (bottom)
                var terrainCell = new ColorRect
                {
                    Position = new Vector2(x * CellSize, y * CellSize),
                    Size = new Vector2(CellSize, CellSize),
                    Color = FloorColor // Default to floor
                };
                AddChild(terrainCell);
                _gridCells[x, y] = terrainCell;

                // FOV overlay layer (top)
                var fovCell = new ColorRect
                {
                    Position = new Vector2(x * CellSize, y * CellSize),
                    Size = new Vector2(CellSize, CellSize),
                    Color = new Color(0, 0, 0, 0), // Transparent by default
                    ZIndex = 10 // Above terrain
                };
                AddChild(fovCell);
                _fovCells[x, y] = fovCell;
            }
        }

        GD.Print($"Created {GridSize}×{GridSize} grid cells");
    }

    private async void InitializeGameState()
    {
        // Create actor IDs
        _playerId = ActorId.NewId();
        _dummyId = ActorId.NewId();
        _activeActorId = _playerId; // Start with player's FOV

        // Initialize test terrain: Walls around edges, smoke patches
        for (int x = 0; x < GridSize; x++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(x, 0), TerrainType.Wall));
            await _mediator.Send(new SetTerrainCommand(new Position(x, GridSize - 1), TerrainType.Wall));
        }

        for (int y = 0; y < GridSize; y++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(0, y), TerrainType.Wall));
            await _mediator.Send(new SetTerrainCommand(new Position(GridSize - 1, y), TerrainType.Wall));
        }

        // Add some smoke patches for testing vision blocking
        await _mediator.Send(new SetTerrainCommand(new Position(10, 10), TerrainType.Smoke));
        await _mediator.Send(new SetTerrainCommand(new Position(10, 11), TerrainType.Smoke));
        await _mediator.Send(new SetTerrainCommand(new Position(11, 10), TerrainType.Smoke));

        // Add some interior walls
        for (int x = 5; x < 10; x++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(x, 15), TerrainType.Wall));
        }

        // Register actors at starting positions
        var playerStartPos = new Position(5, 5);
        var dummyStartPos = new Position(20, 20);

        await _mediator.Send(new RegisterActorCommand(_playerId, playerStartPos));
        await _mediator.Send(new RegisterActorCommand(_dummyId, dummyStartPos));

        // Render all terrain
        RenderAllTerrain();

        // Set initial actor colors
        SetCellColor(playerStartPos.X, playerStartPos.Y, PlayerColor);
        SetCellColor(dummyStartPos.X, dummyStartPos.Y, DummyColor);

        // Calculate initial FOV for player
        await _mediator.Send(new MoveActorCommand(_playerId, playerStartPos));

        GD.Print("Grid Test Scene initialized!");
        GD.Print("Controls: Arrow Keys = Player, WASD = Dummy, Tab = Switch FOV view");
        GD.Print($"Cell size: {CellSize}×{CellSize} pixels, Grid: {GridSize}×{GridSize} cells");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
            return;

        // Tab: Switch which actor's FOV is displayed
        if (keyEvent.Keycode == Key.Tab)
        {
            _activeActorId = _activeActorId.Equals(_playerId) ? _dummyId : _playerId;
            GD.Print($"Switched FOV view to: {(_activeActorId.Equals(_playerId) ? "Player" : "Dummy")}");

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
            GD.PrintErr($"Failed to get actor position: {currentPosResult.Error}");
            return;
        }

        var currentPos = currentPosResult.Value;
        var newPos = new Position(currentPos.X + direction.X, currentPos.Y + direction.Y);

        // Send move command (Core handles validation, FOV calc, events)
        var moveResult = await _mediator.Send(new MoveActorCommand(actorId, newPos));

        if (moveResult.IsFailure)
        {
            GD.Print($"Move blocked: {moveResult.Error}");
        }
        // Success: Events will trigger OnActorMoved + OnFOVCalculated
    }

    /// <summary>
    /// Renders terrain colors for all grid cells.
    /// </summary>
    private void RenderAllTerrain()
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                // Determine terrain color (matches SetTerrainCommand logic)
                Color color;

                // Edges are walls
                if (x == 0 || x == GridSize - 1 || y == 0 || y == GridSize - 1)
                {
                    color = WallColor;
                }
                // Smoke patches
                else if ((x == 10 && y == 10) || (x == 10 && y == 11) || (x == 11 && y == 10))
                {
                    color = SmokeColor;
                }
                // Interior wall
                else if (y == 15 && x >= 5 && x < 10)
                {
                    color = WallColor;
                }
                // Everything else is floor
                else
                {
                    color = FloorColor;
                }

                _gridCells[x, y].Color = color;
            }
        }

        GD.Print("Terrain rendered with pure colors");
    }

    /// <summary>
    /// Event handler: Actor moved - update cell color.
    /// </summary>
    private void OnActorMoved(ActorMovedEvent evt)
    {
        // Get old position to restore terrain color
        var oldPosResult = _mediator.Send(new GetActorPositionQuery(evt.ActorId)).Result;

        // Restore old cell to terrain color (clear actor color)
        if (oldPosResult.IsSuccess)
        {
            var oldPos = oldPosResult.Value;
            // Only restore if we actually moved (old != new)
            if (oldPos.X != evt.NewPosition.X || oldPos.Y != evt.NewPosition.Y)
            {
                RestoreTerrainColor(oldPos.X, oldPos.Y);
            }
        }

        // Set new cell to actor color
        var actorColor = evt.ActorId.Equals(_playerId) ? PlayerColor : DummyColor;
        SetCellColor(evt.NewPosition.X, evt.NewPosition.Y, actorColor);

        GD.Print($"Actor moved to ({evt.NewPosition.X}, {evt.NewPosition.Y})");
    }

    /// <summary>
    /// Event handler: FOV calculated - update visibility overlay.
    /// </summary>
    private void OnFOVCalculated(FOVCalculatedEvent evt)
    {
        if (!evt.ActorId.Equals(_activeActorId))
            return; // Only show active actor's FOV

        // Clear all FOV cells
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                _fovCells[x, y].Color = new Color(0, 0, 0, 0); // Transparent
            }
        }

        // Highlight visible cells
        foreach (var pos in evt.VisiblePositions)
        {
            if (pos.X >= 0 && pos.X < GridSize && pos.Y >= 0 && pos.Y < GridSize)
            {
                _fovCells[pos.X, pos.Y].Color = FOVColor;
            }
        }

        GD.Print($"FOV updated: {evt.VisiblePositions.Count} positions visible");
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
    /// Sets a grid cell to a specific color.
    /// </summary>
    private void SetCellColor(int x, int y, Color color)
    {
        if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
        {
            _gridCells[x, y].Color = color;
        }
    }

    /// <summary>
    /// Restores a cell to its terrain color.
    /// </summary>
    private void RestoreTerrainColor(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return;

        // Determine terrain color (same logic as RenderAllTerrain)
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
}
