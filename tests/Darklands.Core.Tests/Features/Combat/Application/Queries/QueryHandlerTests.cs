using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Application.Commands;
using Darklands.Core.Features.Combat.Application.Queries;
using Darklands.Core.Features.Combat.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Combat.Application.Queries;

[Trait("Category", "Combat")]
[Trait("Category", "Unit")]
public class QueryHandlerTests
{
    private readonly ActorId _playerId = ActorId.NewId();
    private readonly ActorId _enemyId = ActorId.NewId();
    private readonly FakeTurnQueueRepository _repository;
    private readonly FakeEventBus _eventBus;
    private readonly IsInCombatQueryHandler _isInCombatHandler;
    private readonly IsActorScheduledQueryHandler _isScheduledHandler;
    private readonly ScheduleActorCommandHandler _scheduleHandler;

    public QueryHandlerTests()
    {
        _repository = new FakeTurnQueueRepository(_playerId);
        _eventBus = new FakeEventBus();
        var playerContext = new FakePlayerContext(_playerId);
        _isInCombatHandler = new IsInCombatQueryHandler(
            _repository,
            NullLogger<IsInCombatQueryHandler>.Instance);
        _isScheduledHandler = new IsActorScheduledQueryHandler(
            _repository,
            NullLogger<IsActorScheduledQueryHandler>.Instance);
        _scheduleHandler = new ScheduleActorCommandHandler(
            _repository,
            _eventBus,
            playerContext,
            NullLogger<ScheduleActorCommandHandler>.Instance);
    }

    [Fact]
    public async Task IsInCombatQuery_ExplorationMode_ShouldReturnFalse()
    {
        // WHY: Only player in queue = exploration mode
        var query = new IsInCombatQuery();

        var result = await _isInCombatHandler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task IsInCombatQuery_CombatMode_ShouldReturnTrue()
    {
        // Arrange: Enter combat by scheduling enemy
        await _scheduleHandler.Handle(
            new ScheduleActorCommand(_enemyId, TimeUnits.Zero),
            CancellationToken.None);

        // Act
        var query = new IsInCombatQuery();
        var result = await _isInCombatHandler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsActorScheduledQuery_ScheduledActor_ShouldReturnTrue()
    {
        // Arrange: Schedule enemy
        await _scheduleHandler.Handle(
            new ScheduleActorCommand(_enemyId, TimeUnits.Zero),
            CancellationToken.None);

        // Act
        var query = new IsActorScheduledQuery(_enemyId);
        var result = await _isScheduledHandler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsActorScheduledQuery_UnscheduledActor_ShouldReturnFalse()
    {
        var query = new IsActorScheduledQuery(_enemyId);

        var result = await _isScheduledHandler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task IsActorScheduledQuery_PlayerAlwaysScheduled_ShouldReturnTrue()
    {
        // WHY: Player permanently in queue (exploration mode)
        var query = new IsActorScheduledQuery(_playerId);

        var result = await _isScheduledHandler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue(); // Player always scheduled
    }
}
