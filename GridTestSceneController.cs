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
/// Controller for Grid FOV Test Scene.
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

    private TileMapLayer _terrainLayer = null!;
    private TileMapLayer _fovLayer = null!;
    private Sprite2D _playerSprite = null!;
    private Sprite2D _dummySprite = null!;

    private ActorId _playerId;
    private ActorId _dummyId;
    private ActorId _activeActorId; // Whose FOV is currently displayed (toggle with Tab)

    private const int TileSize = 8;
    private const int VisionRadius = 8;

    public override void _Ready()
    {
        // ADR-002: ServiceLocator ONLY in _Ready() to bridge Godot → DI
        _mediator = ServiceLocator.Get<IMediator>();
        _eventBus = ServiceLocator.Get<IGodotEventBus>();

        // Get scene nodes
        _terrainLayer = GetNode<TileMapLayer>("TerrainLayer");
        _fovLayer = GetNode<TileMapLayer>("FOVLayer");
        _playerSprite = GetNode<Sprite2D>("Player");
        _dummySprite = GetNode<Sprite2D>("Dummy");

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

    private async void InitializeGameState()
    {
        // Create actor IDs
        _playerId = ActorId.NewId();
        _dummyId = ActorId.NewId();
        _activeActorId = _playerId; // Start with player's FOV

        // Initialize test terrain: Walls around edges, smoke patches
        for (int x = 0; x < 30; x++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(x, 0), TerrainType.Wall));
            await _mediator.Send(new SetTerrainCommand(new Position(x, 29), TerrainType.Wall));
        }

        for (int y = 0; y < 30; y++)
        {
            await _mediator.Send(new SetTerrainCommand(new Position(0, y), TerrainType.Wall));
            await _mediator.Send(new SetTerrainCommand(new Position(29, y), TerrainType.Wall));
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

        // Update sprites to initial positions
        _playerSprite.Position = GridToPixel(playerStartPos);
        _dummySprite.Position = GridToPixel(dummyStartPos);

        // Calculate initial FOV for player
        await _mediator.Send(new MoveActorCommand(_playerId, playerStartPos)); // Triggers FOV calc

        GD.Print("Grid Test Scene initialized!");
        GD.Print("Controls: Arrow Keys = Player, WASD = Dummy, Tab = Switch FOV view");
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
    /// Event handler: Actor moved - update sprite position.
    /// </summary>
    private void OnActorMoved(ActorMovedEvent evt)
    {
        var sprite = evt.ActorId.Equals(_playerId) ? _playerSprite : _dummySprite;
        sprite.Position = GridToPixel(evt.NewPosition);

        GD.Print($"Actor moved to ({evt.NewPosition.X}, {evt.NewPosition.Y})");
    }

    /// <summary>
    /// Event handler: FOV calculated - update visibility overlay.
    /// Only update if this is the active actor's FOV.
    /// </summary>
    private void OnFOVCalculated(FOVCalculatedEvent evt)
    {
        if (!evt.ActorId.Equals(_activeActorId))
            return; // Only show active actor's FOV

        // Clear previous FOV overlay
        _fovLayer.Clear();

        // Highlight visible tiles
        foreach (var pos in evt.VisiblePositions)
        {
            _fovLayer.SetCell(new Vector2I(pos.X, pos.Y), 0, Vector2I.Zero); // FOV tile
        }

        GD.Print($"FOV updated: {evt.VisiblePositions.Count} positions visible");
    }

    private Vector2 GridToPixel(Position gridPos)
    {
        return new Vector2(gridPos.X * TileSize, gridPos.Y * TileSize);
    }
}
