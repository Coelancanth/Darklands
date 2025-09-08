using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Serilog;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Application.Actor.Commands;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Infrastructure.DependencyInjection;
using LanguageExt;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Combat.Commands;

/// <summary>
/// Integration tests for ExecuteAttackCommandHandler verifying end-to-end attack processing
/// with real service implementations and full DI container resolution.
/// 
/// Phase 3 Focus: Infrastructure coordination, data flow validation, service integration
/// </summary>
[Trait("Category", "Phase3")]
[Trait("Category", "Integration")]
public class ExecuteAttackCommandHandlerIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly IGridStateService _gridStateService;
    private readonly IActorStateService _actorStateService;
    private readonly ICombatSchedulerService _combatSchedulerService;

    public ExecuteAttackCommandHandlerIntegrationTests()
    {
        // Initialize GameStrapper with test configuration
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
        _serviceProvider = initResult.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}"));

        // Resolve services from DI container
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _gridStateService = _serviceProvider.GetRequiredService<IGridStateService>();
        _actorStateService = _serviceProvider.GetRequiredService<IActorStateService>();
        _combatSchedulerService = _serviceProvider.GetRequiredService<ICombatSchedulerService>();
    }

    [Fact]
    public async Task Handle_CompleteAttackFlow_ProcessesEndToEnd()
    {
        // Arrange: Set up actors and grid positions
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();
        var attackerPosition = new Position(2, 2);
        var targetPosition = new Position(2, 3); // Adjacent

        // Create actors with full health
        var attackerResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(targetId, 50, "Orc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add actors to services
        _actorStateService.AddActor(attacker);
        _actorStateService.AddActor(target);
        _gridStateService.AddActorToGrid(attackerId, attackerPosition);
        _gridStateService.AddActorToGrid(targetId, targetPosition);

        // Schedule attacker (target will be scheduled later if needed)
        _combatSchedulerService.ScheduleActor(attackerId, attackerPosition, TimeUnit.CreateUnsafe(100));

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash);

        // Act: Execute attack through MediatR pipeline
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert: Verify successful attack execution
        result.IsSucc.Should().BeTrue("Attack should succeed with valid adjacent targets");

        // Verify target took damage
        var targetAfterAttack = _actorStateService.GetActor(targetId);
        targetAfterAttack.IsSome.Should().BeTrue("Target should still exist in state service");

        targetAfterAttack.IfSome(actor =>
        {
            actor.Health.Current.Should().BeLessThan(50, "Target should have taken damage");
            actor.Health.Current.Should().Be(50 - CombatAction.Common.SwordSlash.BaseDamage, "Damage should match SwordSlash base damage");
        });

        // Verify attacker position unchanged
        var attackerPositionAfter = _gridStateService.GetActorPosition(attackerId);
        attackerPositionAfter.IsSome.Should().BeTrue("Attacker should remain on grid");
        attackerPositionAfter.IfSome(pos => pos.Should().Be(attackerPosition, "Attacker position should not change"));
    }

    [Fact]
    public async Task Handle_LethalAttack_KillsTargetAndCleansUp()
    {
        // Arrange: Set up attacker and weak target
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();
        var attackerPosition = new Position(1, 1);
        var targetPosition = new Position(1, 2); // Adjacent

        var attackerResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(targetId, 5, "WeakOrc"); // Very low health

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        _actorStateService.AddActor(attacker);
        _actorStateService.AddActor(target);
        _gridStateService.AddActorToGrid(attackerId, attackerPosition);
        _gridStateService.AddActorToGrid(targetId, targetPosition);

        // Schedule both actors
        _combatSchedulerService.ScheduleActor(attackerId, attackerPosition, TimeUnit.CreateUnsafe(100));
        _combatSchedulerService.ScheduleActor(targetId, targetPosition, TimeUnit.CreateUnsafe(200));

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash); // 15 damage

        // Act: Execute lethal attack
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert: Attack succeeded
        result.IsSucc.Should().BeTrue("Lethal attack should succeed");

        // Verify target is dead
        var targetAfterAttack = _actorStateService.GetActor(targetId);
        targetAfterAttack.IsSome.Should().BeTrue("Target should still exist in state service");

        targetAfterAttack.IfSome(actor =>
        {
            actor.IsAlive.Should().BeFalse("Target should be dead after lethal damage");
            actor.Health.IsDead.Should().BeTrue("Target health should show dead state");
        });
    }

    [Fact]
    public async Task Handle_AttackNonAdjacentTarget_FailsValidation()
    {
        // Arrange: Set up actors at non-adjacent positions
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();
        var attackerPosition = new Position(1, 1);
        var targetPosition = new Position(3, 3); // Not adjacent (distance 2,2)

        var attackerResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(targetId, 50, "Orc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        _actorStateService.AddActor(attacker);
        _actorStateService.AddActor(target);
        _gridStateService.AddActorToGrid(attackerId, attackerPosition);
        _gridStateService.AddActorToGrid(targetId, targetPosition);

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash);

        // Act: Attempt attack on distant target
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert: Attack should fail due to range
        result.IsFail.Should().BeTrue("Attack on non-adjacent target should fail");
        result.IfFail(error =>
        {
            error.Message.Should().Contain("not adjacent", "Error should indicate adjacency violation");
        });

        // Verify target took no damage
        var targetAfterAttack = _actorStateService.GetActor(targetId);
        targetAfterAttack.IfSome(actor =>
        {
            actor.Health.Current.Should().Be(50, "Target should not have taken damage from failed attack");
        });
    }

    [Fact]
    public async Task Handle_AttackDeadTarget_FailsValidation()
    {
        // Arrange: Set up attacker and dead target
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();
        var attackerPosition = new Position(2, 2);
        var targetPosition = new Position(2, 3); // Adjacent

        var attackerResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(targetId, 50, "Orc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var aliveTarget = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Kill the target first
        var deadTargetResult = aliveTarget.TakeDamage(100); // More than max health
        var deadTarget = deadTargetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        _actorStateService.AddActor(attacker);
        _actorStateService.AddActor(deadTarget); // Add dead target
        _gridStateService.AddActorToGrid(attackerId, attackerPosition);
        _gridStateService.AddActorToGrid(targetId, targetPosition);

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash);

        // Act: Attempt attack on dead target
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert: Attack should fail due to dead target
        result.IsFail.Should().BeTrue("Attack on dead target should fail");
        result.IfFail(error =>
        {
            error.Message.Should().Contain("dead", "Error should indicate target is dead");
        });
    }

    [Fact]
    public async Task Handle_MultipleSequentialAttacks_MaintainsStateConsistency()
    {
        // Arrange: Set up one attacker and one target for multiple attacks
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();
        var attackerPosition = new Position(3, 3);
        var targetPosition = new Position(3, 4); // Adjacent

        var attackerResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(targetId, 100, "ToughOrc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        _actorStateService.AddActor(attacker);
        _actorStateService.AddActor(target);
        _gridStateService.AddActorToGrid(attackerId, attackerPosition);
        _gridStateService.AddActorToGrid(targetId, targetPosition);

        // Act: Execute multiple attacks
        var firstAttack = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.DaggerStab); // 8 damage
        var secondAttack = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash); // 15 damage
        var thirdAttack = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.AxeChop); // 22 damage

        var firstResult = await _mediator.Send(firstAttack, CancellationToken.None);
        var secondResult = await _mediator.Send(secondAttack, CancellationToken.None);
        var thirdResult = await _mediator.Send(thirdAttack, CancellationToken.None);

        // Assert: All attacks should succeed
        firstResult.IsSucc.Should().BeTrue("First attack should succeed");
        secondResult.IsSucc.Should().BeTrue("Second attack should succeed");
        thirdResult.IsSucc.Should().BeTrue("Third attack should succeed");

        // Verify cumulative damage (8 + 15 + 22 = 45 damage total)
        var finalTarget = _actorStateService.GetActor(targetId);
        finalTarget.IfSome(actor =>
        {
            actor.Health.Current.Should().Be(100 - 45, "Target should have cumulative damage from all attacks");
            actor.IsAlive.Should().BeTrue("Target should still be alive after non-lethal attacks");
        });
    }

    [Fact]
    public void Handle_ServiceIntegration_VerifiesDependencyInjection()
    {
        // Arrange: Verify all required services are properly resolved from DI
        var attackerId = ActorId.NewId();
        var targetId = ActorId.NewId();

        // Act & Assert: Verify service resolution
        _gridStateService.Should().NotBeNull("IGridStateService should be resolved from DI");
        _actorStateService.Should().NotBeNull("IActorStateService should be resolved from DI");
        _combatSchedulerService.Should().NotBeNull("ICombatSchedulerService should be resolved from DI");
        _mediator.Should().NotBeNull("IMediator should be resolved from DI");

        // Verify service types are correct implementations
        _gridStateService.Should().BeOfType<Darklands.Core.Application.Grid.Services.InMemoryGridStateService>();
        _actorStateService.Should().BeOfType<Darklands.Core.Application.Actor.Services.InMemoryActorStateService>();
        _combatSchedulerService.Should().BeOfType<Darklands.Core.Application.Combat.Services.InMemoryCombatSchedulerService>();

        // Verify MediatR can resolve the handler
        var command = ExecuteAttackCommand.Create(attackerId, targetId);
        var handler = _serviceProvider.GetService<IRequestHandler<ExecuteAttackCommand, Fin<LanguageExt.Unit>>>();
        handler.Should().NotBeNull("ExecuteAttackCommandHandler should be resolved from MediatR assembly scanning");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GameStrapper.Dispose();
    }
}
