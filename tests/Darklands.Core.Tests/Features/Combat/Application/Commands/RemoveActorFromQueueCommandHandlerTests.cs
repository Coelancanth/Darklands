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
public class RemoveActorFromQueueCommandHandlerTests
{
    private readonly ActorId _playerId = ActorId.NewId();
    private readonly ActorId _enemyId = ActorId.NewId();
    private readonly FakeTurnQueueRepository _repository;
    private readonly FakeEventBus _eventBus;
    private readonly RemoveActorFromQueueCommandHandler _removeHandler;
    private readonly ScheduleActorCommandHandler _scheduleHandler;

    public RemoveActorFromQueueCommandHandlerTests()
    {
        _repository = new FakeTurnQueueRepository(_playerId);
        _eventBus = new FakeEventBus();
        _removeHandler = new RemoveActorFromQueueCommandHandler(
            _repository,
            _eventBus,
            NullLogger<RemoveActorFromQueueCommandHandler>.Instance);
        _scheduleHandler = new ScheduleActorCommandHandler(
            _repository,
            _eventBus,
            NullLogger<ScheduleActorCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_RemoveEnemy_ShouldSucceedAndPublishEvent()
    {
        // Arrange: Schedule enemy first
        await _scheduleHandler.Handle(
            new ScheduleActorCommand(_enemyId, TimeUnits.Zero),
            CancellationToken.None);

        _eventBus.Clear();

        // Act: Remove enemy
        var command = new RemoveActorFromQueueCommand(_enemyId);
        var result = await _removeHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var queue = (await _repository.GetAsync()).Value;
        queue.Contains(_enemyId).Should().BeFalse();

        var events = _eventBus.GetPublishedEvents<TurnQueueChangedEvent>();
        events.Should().ContainSingle();
        events[0].ActorId.Should().Be(_enemyId);
        events[0].ChangeType.Should().Be(TurnQueueChangeType.ActorRemoved);
    }

    [Fact]
    public async Task Handle_RemoveNonExistentActor_ShouldFail()
    {
        var command = new RemoveActorFromQueueCommand(_enemyId);

        var result = await _removeHandler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");

        // No event on failure
        _eventBus.GetPublishedEvents<TurnQueueChangedEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RemoveLastEnemy_ShouldTriggerExplorationMode()
    {
        // WHY: Combat → Exploration transition (important for UI state)
        // Arrange: Enter combat
        await _scheduleHandler.Handle(
            new ScheduleActorCommand(_enemyId, TimeUnits.Zero),
            CancellationToken.None);

        var queue = (await _repository.GetAsync()).Value;
        queue.IsInCombat.Should().BeTrue();

        _eventBus.Clear();

        // Act: Remove last enemy
        var command = new RemoveActorFromQueueCommand(_enemyId);
        await _removeHandler.Handle(command, CancellationToken.None);

        // Assert: Exploration mode restored
        queue = (await _repository.GetAsync()).Value;
        queue.IsInCombat.Should().BeFalse(); // Combat ended!
        queue.Count.Should().Be(1); // Only player remains

        var events = _eventBus.GetPublishedEvents<TurnQueueChangedEvent>();
        events[0].IsInCombat.Should().BeFalse(); // Event reflects mode change
    }

    [Fact]
    public async Task Handle_RemoveOneOfMultipleEnemies_ShouldStayInCombat()
    {
        // WHY: Combat continues until ALL enemies defeated
        var enemy2Id = ActorId.NewId();

        await _scheduleHandler.Handle(
            new ScheduleActorCommand(_enemyId, TimeUnits.Zero),
            CancellationToken.None);
        await _scheduleHandler.Handle(
            new ScheduleActorCommand(enemy2Id, TimeUnits.Zero),
            CancellationToken.None);

        _eventBus.Clear();

        // Remove only first enemy
        await _removeHandler.Handle(
            new RemoveActorFromQueueCommand(_enemyId),
            CancellationToken.None);

        var queue = (await _repository.GetAsync()).Value;
        queue.IsInCombat.Should().BeTrue(); // Still in combat
        queue.Count.Should().Be(2); // Player + enemy2

        var events = _eventBus.GetPublishedEvents<TurnQueueChangedEvent>();
        events[0].IsInCombat.Should().BeTrue();
    }
}
