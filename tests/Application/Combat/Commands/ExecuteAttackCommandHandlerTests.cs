using FluentAssertions;
using Xunit;
using Moq;
using Serilog;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Combat.Commands;

/// <summary>
/// Tests for ExecuteAttackCommandHandler - validates attack execution orchestration.
/// Covers service coordination, validation, damage application, and scheduler updates.
/// Following TDD+VSA Comprehensive Development Workflow.
/// </summary>
[Trait("Category", "Phase2")]
public class ExecuteAttackCommandHandlerTests
{
    private readonly ActorId _attackerId = ActorId.NewId();
    private readonly ActorId _targetId = ActorId.NewId();
    private readonly Position _attackerPosition = new(2, 2);
    private readonly Position _targetPosition = new(2, 3); // Adjacent
    private readonly CombatAction _combatAction = CombatAction.Common.SwordSlash;
    private readonly Darklands.Core.Domain.Actor.Actor _attacker = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(ActorId.NewId(), 100, "Warrior").Match(a => a, _ => throw new System.Exception("Test setup failed"));
    private readonly Darklands.Core.Domain.Actor.Actor _target = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(ActorId.NewId(), 80, "Orc").Match(a => a, _ => throw new System.Exception("Test setup failed"));

    private readonly Mock<IGridStateService> _gridStateService = new();
    private readonly Mock<IActorStateService> _actorStateService = new();
    private readonly Mock<ICombatSchedulerService> _combatSchedulerService = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger> _logger = new();

    [Fact]
    public async Task Handle_ValidAttack_ReturnsSuccess()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);

        SetupValidAttackScenario();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSucc.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AttackerNotFound_ReturnsError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);

        _gridStateService.Setup(x => x.GetActorPosition(_attackerId))
            .Returns(Option<Position>.None);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("not found on grid"));
    }

    [Fact]
    public async Task Handle_TargetNotFound_ReturnsError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);

        _gridStateService.Setup(x => x.GetActorPosition(_attackerId))
            .Returns(Some(_attackerPosition));
        _gridStateService.Setup(x => x.GetActorPosition(_targetId))
            .Returns(Option<Position>.None);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("not found on grid"));
    }

    [Fact]
    public async Task Handle_TargetNotAdjacent_ReturnsError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);
        var distantPosition = new Position(5, 5); // Not adjacent

        _gridStateService.Setup(x => x.GetActorPosition(_attackerId))
            .Returns(Some(_attackerPosition));
        _gridStateService.Setup(x => x.GetActorPosition(_targetId))
            .Returns(Some(distantPosition));
        _actorStateService.Setup(x => x.GetActor(_targetId))
            .Returns(Some(_target));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("not adjacent"));
    }

    [Fact]
    public async Task Handle_TargetDead_ReturnsError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);
        var deadTarget = _target.TakeDamage(1000).Match(a => a, _ => throw new System.Exception("Test setup failed")); // Kill target

        _gridStateService.Setup(x => x.GetActorPosition(_attackerId))
            .Returns(Some(_attackerPosition));
        _gridStateService.Setup(x => x.GetActorPosition(_targetId))
            .Returns(Some(_targetPosition));
        _actorStateService.Setup(x => x.GetActor(_targetId))
            .Returns(Some(deadTarget));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("dead"));
    }

    [Fact]
    public async Task Handle_SelfTarget_ReturnsError()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _attackerId, _combatAction); // Self-target

        _gridStateService.Setup(x => x.GetActorPosition(_attackerId))
            .Returns(Some(_attackerPosition));
        _actorStateService.Setup(x => x.GetActor(_attackerId))
            .Returns(Some(_attacker));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("cannot attack itself"));
    }

    [Fact]
    public async Task Handle_ValidAttack_AppliesDamage()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);

        SetupValidAttackScenario();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Should send DamageActorCommand
        _mediator.Verify(x => x.Send(
            It.IsAny<IRequest<Fin<LanguageExt.Unit>>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidAttack_ReschedulesAttacker()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);

        SetupValidAttackScenario();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Should reschedule attacker with time cost
        _combatSchedulerService.Verify(x => x.ScheduleActor(
            _attackerId,
            _attackerPosition,
            It.IsAny<TimeUnit>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TargetDies_ChecksForCleanup()
    {
        // Arrange
        var handler = CreateHandler();
        var command = ExecuteAttackCommand.Create(_attackerId, _targetId, _combatAction);
        var weakTarget = _target.TakeDamage(75).Match(a => a, _ => throw new System.Exception("Test setup failed")); // 5 health left

        SetupValidAttackScenario();
        _actorStateService.Setup(x => x.GetActor(_targetId))
            .Returns(Some(weakTarget));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Should check if target died after attack
        _actorStateService.Verify(x => x.GetActor(_targetId), Times.AtLeast(1));
    }

    private ExecuteAttackCommandHandler CreateHandler()
    {
        return new ExecuteAttackCommandHandler(
            _gridStateService.Object,
            _actorStateService.Object,
            _combatSchedulerService.Object,
            _mediator.Object,
            _logger.Object);
    }

    private void SetupValidAttackScenario()
    {
        // Setup grid positions
        _gridStateService.Setup(x => x.GetActorPosition(_attackerId))
            .Returns(Some(_attackerPosition));
        _gridStateService.Setup(x => x.GetActorPosition(_targetId))
            .Returns(Some(_targetPosition));

        // Setup actors
        _actorStateService.Setup(x => x.GetActor(_targetId))
            .Returns(Some(_target));
        _actorStateService.Setup(x => x.GetActor(_attackerId))
            .Returns(Some(_attacker));

        // Setup successful damage application
        _mediator.Setup(x => x.Send(It.IsAny<IRequest<Fin<LanguageExt.Unit>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinSucc(LanguageExt.Unit.Default));

        // Setup successful scheduler operations
        _combatSchedulerService.Setup(x => x.ScheduleActor(It.IsAny<ActorId>(), It.IsAny<Position>(), It.IsAny<TimeUnit>()))
            .Returns(FinSucc(LanguageExt.Unit.Default));
    }
}
