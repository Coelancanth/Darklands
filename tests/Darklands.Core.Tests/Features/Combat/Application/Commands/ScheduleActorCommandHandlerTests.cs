using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Application.Commands;
using Darklands.Core.Features.Combat.Domain;
using Darklands.Core.Features.Combat.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Combat.Application.Commands;

[Trait("Category", "Combat")]
[Trait("Category", "Unit")]
public class ScheduleActorCommandHandlerTests
{
    private readonly ActorId _playerId = ActorId.NewId();
    private readonly ActorId _enemyId = ActorId.NewId();
    private readonly FakeTurnQueueRepository _repository;
    private readonly FakeEventBus _eventBus;
    private readonly ScheduleActorCommandHandler _handler;

    public ScheduleActorCommandHandlerTests()
    {
        _repository = new FakeTurnQueueRepository(_playerId);
        _eventBus = new FakeEventBus();
        _handler = new ScheduleActorCommandHandler(
            _repository,
            _eventBus,
            new FakePlayerContext(_playerId),
            NullLogger<ScheduleActorCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_NewEnemy_ShouldScheduleAndPublishEvent()
    {
        var command = new ScheduleActorCommand(_enemyId, TimeUnits.Zero, IsPlayer: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Verify queue state
        var queue = (await _repository.GetAsync()).Value;
        queue.Contains(_enemyId).Should().BeTrue();
        queue.IsInCombat.Should().BeTrue(); // Player + Enemy = combat

        // Verify event published
        var events = _eventBus.GetPublishedEvents<TurnQueueChangedEvent>();
        events.Should().ContainSingle();
        events[0].ActorId.Should().Be(_enemyId);
        events[0].ChangeType.Should().Be(TurnQueueChangeType.ActorScheduled);
        events[0].IsInCombat.Should().BeTrue();
        events[0].QueueSize.Should().Be(2);
    }

    [Fact]
    public async Task Handle_DuplicateActor_ShouldFail()
    {
        // Schedule enemy first time
        var command1 = new ScheduleActorCommand(_enemyId, TimeUnits.Zero);
        await _handler.Handle(command1, CancellationToken.None);

        _eventBus.Clear();

        // Try to schedule same enemy again
        var command2 = new ScheduleActorCommand(_enemyId, TimeUnits.Create(100).Value);
        var result = await _handler.Handle(command2, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already scheduled");

        // No event should be published on failure
        _eventBus.GetPublishedEvents<TurnQueueChangedEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PlayerScheduled_ShouldSetIsPlayerFlag()
    {
        var playerId2 = ActorId.NewId();
        var command = new ScheduleActorCommand(playerId2, TimeUnits.Zero, IsPlayer: true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Verify isPlayer flag propagated
        var queue = (await _repository.GetAsync()).Value;
        var actors = queue.ScheduledActors;
        actors.Should().Contain(a => a.ActorId == playerId2 && a.IsPlayer);
    }

    [Fact]
    public async Task Handle_ExplorationToCombatTransition_ShouldReflectInEvent()
    {
        // WHY: Queue size 1 → 2 triggers combat mode (important for movement cancellation)
        var command = new ScheduleActorCommand(_enemyId, TimeUnits.Zero);

        await _handler.Handle(command, CancellationToken.None);

        var events = _eventBus.GetPublishedEvents<TurnQueueChangedEvent>();
        events.Should().ContainSingle();
        events[0].IsInCombat.Should().BeTrue(); // Transition to combat!
        events[0].QueueSize.Should().Be(2);
    }
}
