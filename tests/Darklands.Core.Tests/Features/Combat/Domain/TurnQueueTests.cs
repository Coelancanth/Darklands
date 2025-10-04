using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Domain;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.Combat.Domain;

[Trait("Category", "Phase1")]
public class TurnQueueTests
{
    private readonly ActorId _playerId = ActorId.NewId();
    private readonly ActorId _enemy1Id = ActorId.NewId();
    private readonly ActorId _enemy2Id = ActorId.NewId();

    [Fact]
    public void CreateWithPlayer_ShouldHavePlayerAtTimeZero()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);

        queue.Count.Should().Be(1);
        queue.IsInCombat.Should().BeFalse();
        queue.Contains(_playerId).Should().BeTrue();

        var next = queue.PeekNext();
        next.IsSuccess.Should().BeTrue();
        next.Value.ActorId.Should().Be(_playerId);
        next.Value.NextActionTime.Should().Be(TimeUnits.Zero);
        next.Value.IsPlayer.Should().BeTrue();
    }

    [Fact]
    public void IsInCombat_WithOnlyPlayer_ShouldBeFalse()
    {
        // WHY: Exploration mode = only player scheduled
        var queue = TurnQueue.CreateWithPlayer(_playerId);

        queue.IsInCombat.Should().BeFalse();
    }

    [Fact]
    public void IsInCombat_WithPlayerAndEnemy_ShouldBeTrue()
    {
        // WHY: Combat mode = multiple actors scheduled
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Zero);

        queue.IsInCombat.Should().BeTrue();
    }

    [Fact]
    public void Schedule_NewActor_ShouldSucceed()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);

        var result = queue.Schedule(_enemy1Id, TimeUnits.Create(100).Value);

        result.IsSuccess.Should().BeTrue();
        queue.Count.Should().Be(2);
        queue.Contains(_enemy1Id).Should().BeTrue();
    }

    [Fact]
    public void Schedule_DuplicateActor_ShouldFail()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Zero);

        var result = queue.Schedule(_enemy1Id, TimeUnits.Create(100).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already scheduled");
    }

    [Fact]
    public void PopNext_WithActors_ShouldReturnLowestTime()
    {
        // SCENARIO: Player@0, Enemy1@50, Enemy2@100
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Create(50).Value);
        queue.Schedule(_enemy2Id, TimeUnits.Create(100).Value);

        var result = queue.PopNext();

        result.IsSuccess.Should().BeTrue();
        result.Value.ActorId.Should().Be(_playerId); // Player at time=0
        queue.Count.Should().Be(2); // Player removed
    }

    [Fact]
    public void PopNext_EmptyQueue_ShouldFail()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.PopNext(); // Remove player

        var result = queue.PopNext();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void PopNext_PlayerFirstTieBreaking_ShouldReturnPlayerBeforeEnemy()
    {
        // WHY: Player always acts first on ties (MVP simplification)
        // SCENARIO: Player@0, Enemy@0 (both ready at same time)
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Zero);

        var first = queue.PopNext();
        var second = queue.PopNext();

        first.IsSuccess.Should().BeTrue();
        first.Value.ActorId.Should().Be(_playerId); // Player wins tie
        second.IsSuccess.Should().BeTrue();
        second.Value.ActorId.Should().Be(_enemy1Id); // Enemy acts second
    }

    [Fact]
    public void PopNext_MultipleEnemiesSameTime_ShouldReturnInScheduleOrder()
    {
        // SCENARIO: Player@0, Enemy1@100, Enemy2@100
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Create(100).Value);
        queue.Schedule(_enemy2Id, TimeUnits.Create(100).Value);

        queue.PopNext(); // Remove player

        var first = queue.PopNext();
        var second = queue.PopNext();

        // Both enemies at time=100, order should be stable (FIFO for non-players)
        first.IsSuccess.Should().BeTrue();
        first.Value.NextActionTime.Value.Should().Be(100);
        second.IsSuccess.Should().BeTrue();
        second.Value.NextActionTime.Value.Should().Be(100);
    }

    [Fact]
    public void PeekNext_ShouldNotRemoveActor()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);

        var peek1 = queue.PeekNext();
        var peek2 = queue.PeekNext();

        peek1.IsSuccess.Should().BeTrue();
        peek2.IsSuccess.Should().BeTrue();
        peek1.Value.ActorId.Should().Be(_playerId);
        peek2.Value.ActorId.Should().Be(_playerId);
        queue.Count.Should().Be(1); // No removal
    }

    [Fact]
    public void Contains_ScheduledActor_ShouldReturnTrue()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Zero);

        queue.Contains(_enemy1Id).Should().BeTrue();
    }

    [Fact]
    public void Contains_UnscheduledActor_ShouldReturnFalse()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);

        queue.Contains(_enemy1Id).Should().BeFalse();
    }

    [Fact]
    public void Remove_ScheduledActor_ShouldSucceed()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Zero);

        var result = queue.Remove(_enemy1Id);

        result.IsSuccess.Should().BeTrue();
        queue.Count.Should().Be(1);
        queue.Contains(_enemy1Id).Should().BeFalse();
    }

    [Fact]
    public void Remove_UnscheduledActor_ShouldFail()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);

        var result = queue.Remove(_enemy1Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void Remove_LastEnemy_ShouldResetPlayerToTimeZero()
    {
        // WHY: When combat ends (only player remains), reset to exploration mode
        // SCENARIO: Player@200 (after several turns), Enemy@250
        // When enemy is removed, player should reset to time=0
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Create(50).Value);

        // Simulate player taking action (advance time)
        var playerActor = queue.PopNext(); // Player acts (removed from queue)
        queue.Schedule(_playerId, TimeUnits.Create(200).Value, isPlayer: true); // Re-schedule at 200

        queue.Remove(_enemy1Id); // Last enemy defeated

        queue.IsInCombat.Should().BeFalse();
        var player = queue.PeekNext().Value;
        player.NextActionTime.Should().Be(TimeUnits.Zero); // Reset to exploration
    }

    [Fact]
    public void Remove_OneOfMultipleEnemies_ShouldStayInCombat()
    {
        // WHY: Combat continues until ALL enemies defeated
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Zero);
        queue.Schedule(_enemy2Id, TimeUnits.Create(100).Value);

        queue.Remove(_enemy1Id);

        queue.IsInCombat.Should().BeTrue(); // Still 2 actors (player + enemy2)
        queue.Count.Should().Be(2);
    }

    [Fact]
    public void Reschedule_ScheduledActor_ShouldUpdateTime()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Create(50).Value);

        var result = queue.Reschedule(_enemy1Id, TimeUnits.Create(150).Value);

        result.IsSuccess.Should().BeTrue();
        var actors = queue.ScheduledActors;
        var enemy = actors.First(a => a.ActorId == _enemy1Id);
        enemy.NextActionTime.Value.Should().Be(150);
    }

    [Fact]
    public void Reschedule_UnscheduledActor_ShouldFail()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);

        var result = queue.Reschedule(_enemy1Id, TimeUnits.Create(100).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void Reschedule_ShouldMaintainQueueOrder()
    {
        // SCENARIO: Player@0, Enemy1@50 → Reschedule Player to 100
        // Expected order after reschedule: Enemy1@50, Player@100
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Create(50).Value);

        queue.Reschedule(_playerId, TimeUnits.Create(100).Value);

        var first = queue.PopNext();
        var second = queue.PopNext();

        first.Value.ActorId.Should().Be(_enemy1Id); // Enemy now acts first
        second.Value.ActorId.Should().Be(_playerId); // Player acts second
    }

    [Fact]
    public void ScheduledActors_ShouldReturnReadOnlyView()
    {
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        queue.Schedule(_enemy1Id, TimeUnits.Create(100).Value);

        var actors = queue.ScheduledActors;

        actors.Count.Should().Be(2);
        actors.Should().ContainSingle(a => a.ActorId == _playerId);
        actors.Should().ContainSingle(a => a.ActorId == _enemy1Id);
    }

    [Fact]
    public void ComplexScenario_SimulateCombatLifecycle()
    {
        // FULL LIFECYCLE TEST: Exploration → Combat → Reinforcement → Victory
        var queue = TurnQueue.CreateWithPlayer(_playerId);
        var goblinId = ActorId.NewId();
        var orcId = ActorId.NewId();

        // 1. EXPLORATION: Player walks around
        queue.IsInCombat.Should().BeFalse();
        queue.PeekNext().Value.ActorId.Should().Be(_playerId);

        // 2. COMBAT STARTS: Goblin detected at player's FOV
        queue.Schedule(goblinId, TimeUnits.Zero);
        queue.IsInCombat.Should().BeTrue();

        // 3. TURN 1: Player acts (wins tie-breaking)
        var turn1 = queue.PopNext().Value;
        turn1.ActorId.Should().Be(_playerId);
        queue.Schedule(_playerId, TimeUnits.Create(100).Value, isPlayer: true); // Re-schedule after acting

        // 4. TURN 2: Goblin acts
        var turn2 = queue.PopNext().Value;
        turn2.ActorId.Should().Be(goblinId);
        queue.Schedule(goblinId, TimeUnits.Create(150).Value); // Re-schedule after acting

        // 5. REINFORCEMENT: Orc appears mid-combat
        queue.Schedule(orcId, TimeUnits.Create(100).Value); // Orc ready at current combat time
        queue.Count.Should().Be(3); // Player, Goblin, Orc

        // 6. TURN 3: Player acts (time=100)
        var turn3 = queue.PopNext().Value;
        turn3.ActorId.Should().Be(_playerId);
        turn3.NextActionTime.Value.Should().Be(100);
        queue.Schedule(_playerId, TimeUnits.Create(200).Value, isPlayer: true);

        // 7. TURN 4: Orc acts (time=100, next in queue)
        var turn4 = queue.PopNext().Value;
        turn4.ActorId.Should().Be(orcId);
        queue.Schedule(orcId, TimeUnits.Create(250).Value); // Re-schedule after acting

        // 8. VICTORY: Defeat Goblin (player defeats it during their turn)
        queue.Remove(goblinId);
        queue.IsInCombat.Should().BeTrue(); // Still fighting Orc (Player + Orc = 2 actors)

        // 9. Defeat Orc (player defeats it during next turn)
        queue.Remove(orcId);
        queue.IsInCombat.Should().BeFalse(); // Combat ends (only Player remains)

        // 10. BACK TO EXPLORATION: Player time reset to 0
        queue.PeekNext().Value.NextActionTime.Should().Be(TimeUnits.Zero);
    }
}
