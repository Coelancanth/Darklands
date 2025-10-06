using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Components;
using Darklands.Core.Domain.Entities;
using Darklands.Core.Features.Combat.Application;
using Darklands.Core.Features.Combat.Application.Commands;
using Darklands.Core.Features.Combat.Domain;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Features.Combat.Application.Commands;

/// <summary>
/// Tests for ExecuteAttackCommandHandler.
/// Validates attack logic: range checking, damage application, time costs, death handling.
/// </summary>
public class ExecuteAttackCommandHandlerTests
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorPositionService _positionService;
    private readonly ITurnQueueRepository _turnQueueRepository;
    private readonly IFOVService _fovService;
    private readonly GridMap _gridMap;
    private readonly ILogger<ExecuteAttackCommandHandler> _logger;
    private readonly ExecuteAttackCommandHandler _handler;

    public ExecuteAttackCommandHandlerTests()
    {
        _actorRepository = Substitute.For<IActorRepository>();
        _positionService = Substitute.For<IActorPositionService>();
        _turnQueueRepository = Substitute.For<ITurnQueueRepository>();
        _fovService = Substitute.For<IFOVService>();
        _logger = Substitute.For<ILogger<ExecuteAttackCommandHandler>>();

        // Create GridMap with floor terrain using stub repository
        var terrainRepo = new StubTerrainRepository();
        var floorTerrain = terrainRepo.GetDefault().Value;
        _gridMap = new GridMap(floorTerrain);

        _handler = new ExecuteAttackCommandHandler(
            _actorRepository,
            _positionService,
            _turnQueueRepository,
            _fovService,
            _gridMap,
            _logger);
    }

    [Fact]
    public async Task Handle_ValidMeleeAttack_ShouldDealDamageAndAdvanceTime()
    {
        // WHY: Core combat flow - attacker deals damage, consumes time units, target survives

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 15, timeCost: 100, range: 1, WeaponType.Melee);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        // Actors are adjacent (distance = 1)
        _positionService.GetPosition(attackerId).Returns(Result.Success(new Position(5, 5)));
        _positionService.GetPosition(targetId).Returns(Result.Success(new Position(6, 5))); // Adjacent

        var turnQueue = TurnQueue.CreateWithPlayer(attackerId);
        turnQueue.Schedule(targetId, TimeUnits.Zero);
        _turnQueueRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(turnQueue));
        _turnQueueRepository.SaveAsync(Arg.Any<TurnQueue>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value.DamageDealt); // Weapon damage
        Assert.False(result.Value.TargetDied); // Target survives (50 - 15 = 35 HP)
        Assert.Equal(35, result.Value.TargetRemainingHealth);

        // Verify time advanced (attacker consumed 100 time units)
        await _turnQueueRepository.Received(1).SaveAsync(
            Arg.Is<TurnQueue>(q => q.ScheduledActors.Any(a =>
                a.ActorId == attackerId && a.NextActionTime.Value == 100)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LethalDamage_ShouldKillTargetAndRemoveFromQueue()
    {
        // WHY: Death handling - target reaches 0 HP and is removed from combat

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 50, timeCost: 100, range: 1, WeaponType.Melee);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 30, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        _positionService.GetPosition(attackerId).Returns(Result.Success(new Position(5, 5)));
        _positionService.GetPosition(targetId).Returns(Result.Success(new Position(6, 5)));

        var turnQueue = TurnQueue.CreateWithPlayer(attackerId);
        turnQueue.Schedule(targetId, TimeUnits.Zero);
        _turnQueueRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(turnQueue));
        _turnQueueRepository.SaveAsync(Arg.Any<TurnQueue>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.DamageDealt);
        Assert.True(result.Value.TargetDied); // 30 HP - 50 damage = 0 HP (dead)
        Assert.Equal(0, result.Value.TargetRemainingHealth);

        // Verify target removed from queue (SaveAsync called twice: death + time advance)
        await _turnQueueRepository.Received(2).SaveAsync(
            Arg.Any<TurnQueue>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MeleeAttackOutOfRange_ShouldFail()
    {
        // WHY: Range validation prevents melee attacks beyond adjacent tiles

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 15, timeCost: 100, range: 1, WeaponType.Melee);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        // Actors are 2 tiles apart (out of melee range)
        _positionService.GetPosition(attackerId).Returns(Result.Success(new Position(5, 5)));
        _positionService.GetPosition(targetId).Returns(Result.Success(new Position(7, 5))); // Distance = 2

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("out of melee range", result.Error);
    }

    [Fact]
    public async Task Handle_RangedAttackInRange_ShouldSucceed()
    {
        // WHY: Ranged weapons can attack at distance (bow, crossbow)

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 10, timeCost: 120, range: 8, WeaponType.Ranged); // Bow with 8 tile range
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        // Target 5 tiles away (within bow range)
        var attackerPos = new Position(5, 5);
        var targetPos = new Position(10, 5);
        _positionService.GetPosition(attackerId).Returns(Result.Success(attackerPos));
        _positionService.GetPosition(targetId).Returns(Result.Success(targetPos)); // Distance = 5

        // Mock FOV: Target is visible (line-of-sight clear)
        var visiblePositions = new HashSet<Position> { targetPos };
        _fovService.CalculateFOV(_gridMap, attackerPos, 8).Returns(Result.Success(visiblePositions));

        var turnQueue = TurnQueue.CreateWithPlayer(attackerId);
        turnQueue.Schedule(targetId, TimeUnits.Zero);
        _turnQueueRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(turnQueue));
        _turnQueueRepository.SaveAsync(Arg.Any<TurnQueue>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.DamageDealt);
    }

    [Fact]
    public async Task Handle_RangedAttackOutOfRange_ShouldFail()
    {
        // WHY: Ranged weapons have maximum range limits

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 10, timeCost: 120, range: 8, WeaponType.Ranged);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        // Target 10 tiles away (beyond bow range of 8)
        _positionService.GetPosition(attackerId).Returns(Result.Success(new Position(5, 5)));
        _positionService.GetPosition(targetId).Returns(Result.Success(new Position(15, 5))); // Distance = 10

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("out of ranged weapon range", result.Error);
    }

    [Fact]
    public async Task Handle_AttackerHasNoWeapon_ShouldFail()
    {
        // WHY: Cannot attack without weapon equipped

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = new Actor(attackerId, "ACTOR_UNARMED"); // No weapon component!
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("has no weapon equipped", result.Error);
    }

    [Fact]
    public async Task Handle_TargetAlreadyDead_ShouldFail()
    {
        // WHY: Cannot attack corpses (target must be alive)

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 15, timeCost: 100, range: 1, WeaponType.Melee);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 0, maxHealth: 50); // Dead!

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        _positionService.GetPosition(attackerId).Returns(Result.Success(new Position(5, 5)));
        _positionService.GetPosition(targetId).Returns(Result.Success(new Position(6, 5)));

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already dead", result.Error);
    }

    [Fact]
    public async Task Handle_AttackerNotFound_ShouldFail()
    {
        // WHY: Validation - attacker must exist in repository

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Failure<Actor>("Attacker not found"));

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Attacker", result.Error);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task Handle_TargetNotFound_ShouldFail()
    {
        // WHY: Validation - target must exist in repository

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 15, timeCost: 100, range: 1, WeaponType.Melee);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Failure<Actor>("Target not found"));

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Target", result.Error);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task Handle_DiagonalAttack_ShouldTreatAsAdjacentForMelee()
    {
        // WHY: Chebyshev distance (8-directional) - diagonals count as 1 tile for melee

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 15, timeCost: 100, range: 1, WeaponType.Melee);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        // Diagonal position (Chebyshev distance = 1)
        _positionService.GetPosition(attackerId).Returns(Result.Success(new Position(5, 5)));
        _positionService.GetPosition(targetId).Returns(Result.Success(new Position(6, 6))); // Diagonal

        var turnQueue = TurnQueue.CreateWithPlayer(attackerId);
        turnQueue.Schedule(targetId, TimeUnits.Zero);
        _turnQueueRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(turnQueue));
        _turnQueueRepository.SaveAsync(Arg.Any<TurnQueue>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess); // Diagonal is valid for melee (distance = 1)
    }

    // Helper methods

    private Actor CreateActorWithWeapon(
        ActorId id,
        string nameKey,
        float damage,
        int timeCost,
        int range,
        WeaponType type)
    {
        var actor = new Actor(id, nameKey);

        var weapon = Weapon.Create($"WEAPON_{nameKey}", damage, timeCost, range, type).Value;
        actor.AddComponent<IWeaponComponent>(new WeaponComponent(weapon));

        return actor;
    }

    private Actor CreateActorWithHealth(
        ActorId id,
        string nameKey,
        float currentHealth,
        float maxHealth)
    {
        var actor = new Actor(id, nameKey);

        var health = Darklands.Core.Domain.Common.Health.Create(currentHealth, maxHealth).Value;
        actor.AddComponent<IHealthComponent>(new HealthComponent(health));

        return actor;
    }

    // ========== Phase 3: Line-of-Sight Tests ==========

    [Fact]
    public async Task Handle_RangedAttackWithLineOfSightBlocked_ShouldFail()
    {
        // WHY: Walls/obstacles block ranged attacks (Phase 3 - FOV integration)

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 10, timeCost: 120, range: 8, WeaponType.Ranged);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        // Target 5 tiles away (within range) BUT behind a wall
        var attackerPos = new Position(5, 5);
        var targetPos = new Position(10, 5);
        _positionService.GetPosition(attackerId).Returns(Result.Success(attackerPos));
        _positionService.GetPosition(targetId).Returns(Result.Success(targetPos));

        // Mock FOV: Target NOT visible (wall blocks line-of-sight)
        var visiblePositions = new HashSet<Position>(); // Empty set = target not visible
        _fovService.CalculateFOV(_gridMap, attackerPos, 8).Returns(Result.Success(visiblePositions));

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not visible", result.Error);
        Assert.Contains("line-of-sight blocked", result.Error);
    }

    [Fact]
    public async Task Handle_RangedAttackWithClearLineOfSight_ShouldSucceed()
    {
        // WHY: Clear line-of-sight allows ranged attack

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 12, timeCost: 110, range: 10, WeaponType.Ranged);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        var attackerPos = new Position(3, 3);
        var targetPos = new Position(8, 8); // Diagonal, distance = 5
        _positionService.GetPosition(attackerId).Returns(Result.Success(attackerPos));
        _positionService.GetPosition(targetId).Returns(Result.Success(targetPos));

        // Mock FOV: Target IS visible (clear line-of-sight)
        var visiblePositions = new HashSet<Position> { targetPos };
        _fovService.CalculateFOV(_gridMap, attackerPos, 10).Returns(Result.Success(visiblePositions));

        var turnQueue = TurnQueue.CreateWithPlayer(attackerId);
        turnQueue.Schedule(targetId, TimeUnits.Zero);
        _turnQueueRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(turnQueue));
        _turnQueueRepository.SaveAsync(Arg.Any<TurnQueue>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value.DamageDealt);
    }

    [Fact]
    public async Task Handle_MeleeAttackDoesNotCheckLineOfSight_ShouldSucceed()
    {
        // WHY: Melee attacks don't require line-of-sight (can attack around corners, in darkness)

        // Arrange
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        var attacker = CreateActorWithWeapon(attackerId, "ACTOR_PLAYER",
            damage: 20, timeCost: 100, range: 1, WeaponType.Melee);
        var target = CreateActorWithHealth(targetId, "ACTOR_GOBLIN", currentHealth: 50, maxHealth: 50);

        _actorRepository.GetByIdAsync(attackerId).Returns(Result.Success(attacker));
        _actorRepository.GetByIdAsync(targetId).Returns(Result.Success(target));

        // Adjacent positions (melee range)
        _positionService.GetPosition(attackerId).Returns(Result.Success(new Position(5, 5)));
        _positionService.GetPosition(targetId).Returns(Result.Success(new Position(6, 5)));

        var turnQueue = TurnQueue.CreateWithPlayer(attackerId);
        turnQueue.Schedule(targetId, TimeUnits.Zero);
        _turnQueueRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(turnQueue));
        _turnQueueRepository.SaveAsync(Arg.Any<TurnQueue>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var cmd = new ExecuteAttackCommand(attackerId, targetId);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        // Verify FOV was NOT called for melee (melee doesn't check line-of-sight)
        _fovService.DidNotReceive().CalculateFOV(Arg.Any<GridMap>(), Arg.Any<Position>(), Arg.Any<int>());
    }
}
