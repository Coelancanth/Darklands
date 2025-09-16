using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Serilog;
using Darklands.Application.Combat.Commands;
using Darklands.Application.Combat.Services;
using Darklands.Application.Actor.Services;
using Darklands.Application.Actor.Commands;
using Darklands.Application.Grid.Services;
using Darklands.Domain.Combat;
using Darklands.Domain.Grid;
using Darklands.Application.Infrastructure.DependencyInjection;
using Darklands.Core.Tests.TestUtilities;
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
[Collection("GameStrapper")]
public class ExecuteAttackCommandHandlerIntegrationTests : IDisposable
{
    private ServiceProvider? _testServiceProvider;

    private ServiceProvider GetServiceProvider()
    {
        if (_testServiceProvider == null)
        {
            // Ensure GameStrapper is properly disposed before re-initialization to avoid disposal races
            GameStrapper.Dispose();

            var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
            _testServiceProvider = initResult.Match(
                Succ: provider => provider,
                Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}"));
        }
        return _testServiceProvider;
    }

    // Safe service accessors that reuse the same provider within a test
    private IMediator GetMediator() => GetServiceProvider().GetRequiredService<IMediator>();
    private IGridStateService GetGridStateService() => GetServiceProvider().GetRequiredService<IGridStateService>();
    private IActorStateService GetActorStateService() => GetServiceProvider().GetRequiredService<IActorStateService>();
    private ICombatSchedulerService GetCombatSchedulerService() => GetServiceProvider().GetRequiredService<ICombatSchedulerService>();

    public ExecuteAttackCommandHandlerIntegrationTests()
    {
        // Initialize GameStrapper with test configuration
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        // Initialize provider for this test instance
        var _ = GetServiceProvider();
    }

    [Fact]
    public async Task Handle_CompleteAttackFlow_ProcessesEndToEnd()
    {
        // Arrange: Set up actors and grid positions (using open positions from strategic grid)
        var attackerId = ActorId.NewId(TestIdGenerator.Instance);
        var targetId = ActorId.NewId(TestIdGenerator.Instance);
        var attackerPosition = new Position(15, 10); // Player position (guaranteed open)
        var targetPosition = new Position(15, 11); // Adjacent to player position

        // Create actors with full health
        var attackerResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(targetId, 50, "Orc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add actors to services
        GetActorStateService().AddActor(attacker);
        GetActorStateService().AddActor(target);
        GetGridStateService().AddActorToGrid(attackerId, attackerPosition);
        GetGridStateService().AddActorToGrid(targetId, targetPosition);

        // Schedule attacker (target will be scheduled later if needed)
        GetCombatSchedulerService().ScheduleActor(attackerId, attackerPosition, TimeUnit.CreateUnsafe(100));

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash);

        // Act: Execute attack through MediatR pipeline
        var result = await GetMediator().Send(command, CancellationToken.None);

        // Assert: Verify successful attack execution
        result.IsSucc.Should().BeTrue("Attack should succeed with valid adjacent targets");

        // Verify target took damage
        var targetAfterAttack = GetActorStateService().GetActor(targetId);
        targetAfterAttack.IsSome.Should().BeTrue("Target should still exist in state service");

        targetAfterAttack.IfSome(actor =>
        {
            actor.Health.Current.Should().BeLessThan(50, "Target should have taken damage");
            actor.Health.Current.Should().Be(50 - CombatAction.Common.SwordSlash.BaseDamage, "Damage should match SwordSlash base damage");
        });

        // Verify attacker position unchanged
        var attackerPositionAfter = GetGridStateService().GetActorPosition(attackerId);
        attackerPositionAfter.IsSome.Should().BeTrue("Attacker should remain on grid");
        attackerPositionAfter.IfSome(pos => pos.Should().Be(attackerPosition, "Attacker position should not change"));
    }

    [Fact]
    public async Task Handle_LethalAttack_KillsTargetAndCleansUp()
    {
        // Arrange: Set up attacker and weak target
        var attackerId = ActorId.NewId(TestIdGenerator.Instance);
        var targetId = ActorId.NewId(TestIdGenerator.Instance);
        var attackerPosition = new Position(5, 10); // Goblin position (guaranteed open)
        var targetPosition = new Position(5, 11); // Adjacent

        var attackerResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(targetId, 5, "WeakOrc"); // Very low health

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        GetActorStateService().AddActor(attacker);
        GetActorStateService().AddActor(target);
        GetGridStateService().AddActorToGrid(attackerId, attackerPosition);
        GetGridStateService().AddActorToGrid(targetId, targetPosition);

        // Schedule both actors
        GetCombatSchedulerService().ScheduleActor(attackerId, attackerPosition, TimeUnit.CreateUnsafe(100));
        GetCombatSchedulerService().ScheduleActor(targetId, targetPosition, TimeUnit.CreateUnsafe(200));

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash); // 15 damage

        // Act: Execute lethal attack
        var result = await GetMediator().Send(command, CancellationToken.None);

        // Assert: Attack succeeded
        result.IsSucc.Should().BeTrue("Lethal attack should succeed");

        // Verify target is dead
        var targetAfterAttack = GetActorStateService().GetActor(targetId);
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
        var attackerId = ActorId.NewId(TestIdGenerator.Instance);
        var targetId = ActorId.NewId(TestIdGenerator.Instance);
        var attackerPosition = new Position(20, 15); // Orc position (guaranteed open)
        var targetPosition = new Position(23, 18); // Not adjacent (distance 3,3), using open area

        var attackerResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(targetId, 50, "Orc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        GetActorStateService().AddActor(attacker);
        GetActorStateService().AddActor(target);
        GetGridStateService().AddActorToGrid(attackerId, attackerPosition);
        GetGridStateService().AddActorToGrid(targetId, targetPosition);

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash);

        // Act: Attempt attack on distant target
        var result = await GetMediator().Send(command, CancellationToken.None);

        // Assert: Attack should fail due to range
        result.IsFail.Should().BeTrue("Attack on non-adjacent target should fail");
        result.IfFail(error =>
        {
            error.Message.Should().Contain("not adjacent", "Error should indicate adjacency violation");
        });

        // Verify target took no damage
        var targetAfterAttack = GetActorStateService().GetActor(targetId);
        targetAfterAttack.IfSome(actor =>
        {
            actor.Health.Current.Should().Be(50, "Target should not have taken damage from failed attack");
        });
    }

    [Fact]
    public async Task Handle_AttackDeadTarget_FailsValidation()
    {
        // Arrange: Set up attacker and dead target
        var attackerId = ActorId.NewId(TestIdGenerator.Instance);
        var targetId = ActorId.NewId(TestIdGenerator.Instance);
        var attackerPosition = new Position(25, 5); // Eagle position (guaranteed open)
        var targetPosition = new Position(24, 5); // Adjacent (horizontal, moving away from border)

        var attackerResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(targetId, 50, "Orc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var aliveTarget = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Kill the target first
        var deadTargetResult = aliveTarget.TakeDamage(100); // More than max health
        var deadTarget = deadTargetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        GetActorStateService().AddActor(attacker);
        GetActorStateService().AddActor(deadTarget); // Add dead target
        GetGridStateService().AddActorToGrid(attackerId, attackerPosition);
        GetGridStateService().AddActorToGrid(targetId, targetPosition);

        var command = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash);

        // Act: Attempt attack on dead target
        var result = await GetMediator().Send(command, CancellationToken.None);

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
        var attackerId = ActorId.NewId(TestIdGenerator.Instance);
        var targetId = ActorId.NewId(TestIdGenerator.Instance);
        var attackerPosition = new Position(14, 10); // Near player position (guaranteed open)
        var targetPosition = new Position(14, 11); // Adjacent

        var attackerResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(attackerId, 100, "Warrior");
        var targetResult = Darklands.Domain.Actor.Actor.CreateAtFullHealth(targetId, 100, "ToughOrc");

        var attacker = attackerResult.Match(a => a, _ => throw new Exception("Test setup failed"));
        var target = targetResult.Match(a => a, _ => throw new Exception("Test setup failed"));

        // Add to services
        GetActorStateService().AddActor(attacker);
        GetActorStateService().AddActor(target);
        GetGridStateService().AddActorToGrid(attackerId, attackerPosition);
        GetGridStateService().AddActorToGrid(targetId, targetPosition);

        // Act: Execute multiple attacks
        var firstAttack = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.DaggerStab); // 8 damage
        var secondAttack = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.SwordSlash); // 15 damage
        var thirdAttack = ExecuteAttackCommand.Create(attackerId, targetId, CombatAction.Common.AxeChop); // 22 damage

        var firstResult = await GetMediator().Send(firstAttack, CancellationToken.None);
        var secondResult = await GetMediator().Send(secondAttack, CancellationToken.None);
        var thirdResult = await GetMediator().Send(thirdAttack, CancellationToken.None);

        // Assert: All attacks should succeed
        firstResult.IsSucc.Should().BeTrue("First attack should succeed");
        secondResult.IsSucc.Should().BeTrue("Second attack should succeed");
        thirdResult.IsSucc.Should().BeTrue("Third attack should succeed");

        // Verify cumulative damage (8 + 15 + 22 = 45 damage total)
        var finalTarget = GetActorStateService().GetActor(targetId);
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
        var attackerId = ActorId.NewId(TestIdGenerator.Instance);
        var targetId = ActorId.NewId(TestIdGenerator.Instance);

        // Act & Assert: Verify service resolution
        GetGridStateService().Should().NotBeNull("IGridStateService should be resolved from DI");
        GetActorStateService().Should().NotBeNull("IActorStateService should be resolved from DI");
        GetCombatSchedulerService().Should().NotBeNull("ICombatSchedulerService should be resolved from DI");
        GetMediator().Should().NotBeNull("IMediator should be resolved from DI");

        // Verify service types are correct implementations
        GetGridStateService().Should().BeOfType<Darklands.Application.Grid.Services.InMemoryGridStateService>();
        GetActorStateService().Should().BeOfType<Darklands.Application.Actor.Services.InMemoryActorStateService>();
        GetCombatSchedulerService().Should().BeOfType<Darklands.Application.Combat.Services.InMemoryCombatSchedulerService>();

        // Verify MediatR can resolve the handler
        var command = ExecuteAttackCommand.Create(attackerId, targetId);
        var handler = GetServiceProvider().GetService<IRequestHandler<ExecuteAttackCommand, Fin<LanguageExt.Unit>>>();
        handler.Should().NotBeNull("ExecuteAttackCommandHandler should be resolved from MediatR assembly scanning");
    }

    public void Dispose()
    {
        _testServiceProvider?.Dispose();
        _testServiceProvider = null;
        GameStrapper.Dispose();
    }
}
