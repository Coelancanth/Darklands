using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Application.Actor.Commands;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Combat.Commands;

/// <summary>
/// Handler for ExecuteAttackCommand - Orchestrates melee combat between actors.
/// Implements functional CQRS pattern with comprehensive service coordination.
/// 
/// Coordination Flow:
/// 1. Validate attack (positions, adjacency, target alive)
/// 2. Apply damage to target
/// 3. Reschedule attacker with time cost
/// 4. Remove dead actors from scheduler
/// 
/// Following TDD+VSA Comprehensive Development Workflow.
/// </summary>
public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Fin<LanguageExt.Unit>>
{
    private readonly IGridStateService _gridStateService;
    private readonly IActorStateService _actorStateService;
    private readonly ICombatSchedulerService _combatSchedulerService;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IAttackFeedbackService? _attackFeedbackService;

    /// <summary>
    /// Simple callback for actor death notifications.
    /// GameManager can set this to handle visual cleanup.
    /// </summary>
    public static Action<ActorId, Position>? OnActorDeath { get; set; }

    public ExecuteAttackCommandHandler(
        IGridStateService gridStateService,
        IActorStateService actorStateService,
        ICombatSchedulerService combatSchedulerService,
        IMediator mediator,
        ILogger logger,
        IAttackFeedbackService? attackFeedbackService = null)
    {
        _gridStateService = gridStateService;
        _actorStateService = actorStateService;
        _combatSchedulerService = combatSchedulerService;
        _mediator = mediator;
        _logger = logger;
        _attackFeedbackService = attackFeedbackService;
    }

    public async Task<Fin<LanguageExt.Unit>> Handle(ExecuteAttackCommand request, CancellationToken cancellationToken)
    {
        _logger?.Debug("Processing ExecuteAttackCommand: {AttackerId} attacking {TargetId} with {Action}",
            request.AttackerId, request.TargetId, request.CombatAction.Name);

        // Orchestrate the attack using functional composition
        var result = await ExecuteAttackAsync(request);

        return result.Match(
            Succ: _ =>
            {
                // Get target position for logging
                var targetPos = _gridStateService.GetActorPosition(request.TargetId)
                    .Match(p => p, () => new Position(0, 0));

                // Check if target died for combat summary
                var targetAfterAttack = _actorStateService.GetActor(request.TargetId);
                var outcomeMessage = targetAfterAttack.Match(
                    Some: actor => actor.IsAlive ? "hit" : "defeated",
                    None: () => "defeated" // If not found, assume dead
                );

                _logger?.Information("⚔️ {AttackerId} [{Action}] → {TargetId} at ({X},{Y}): {Damage} damage ({Outcome})",
                    request.AttackerId, request.CombatAction.Name, request.TargetId,
                    targetPos.X, targetPos.Y, request.CombatAction.BaseDamage, outcomeMessage);

                return FinSucc(LanguageExt.Unit.Default);
            },
            Fail: error =>
            {
                _logger?.Warning("Attack failed: {AttackerId} -> {TargetId}: {Error}",
                    request.AttackerId, request.TargetId, error.Message);

                // Provide attack failure feedback (sequential, not concurrent)
                if (_attackFeedbackService != null)
                {
                    _attackFeedbackService.ProcessAttackFailureAsync(
                        request.AttackerId,
                        request.TargetId,
                        request.CombatAction,
                        error.Message);
                }

                return FinFail<LanguageExt.Unit>(error);
            }
        );
    }

    private async Task<Fin<LanguageExt.Unit>> ExecuteAttackAsync(ExecuteAttackCommand request)
    {
        // Step 1: Get attacker position
        var attackerPositionOption = _gridStateService.GetActorPosition(request.AttackerId);
        if (attackerPositionOption.IsNone)
        {
            return FinFail<LanguageExt.Unit>(Error.New("Attacker not found on grid"));
        }
        var attackerPosition = attackerPositionOption.IfNone(() => throw new InvalidOperationException("Position should exist"));

        // Step 2: Get target position  
        var targetPositionOption = _gridStateService.GetActorPosition(request.TargetId);
        if (targetPositionOption.IsNone)
        {
            return FinFail<LanguageExt.Unit>(Error.New("Target not found on grid"));
        }
        var targetPosition = targetPositionOption.IfNone(() => throw new InvalidOperationException("Position should exist"));

        // Step 3: Get target actor
        var targetActorOption = _actorStateService.GetActor(request.TargetId);
        if (targetActorOption.IsNone)
        {
            return FinFail<LanguageExt.Unit>(Error.New("Target actor not found"));
        }
        var targetActor = targetActorOption.IfNone(() => throw new InvalidOperationException("Actor should exist"));

        // Step 4: Validate attack
        var validationResult = ValidateAttack(request.AttackerId, attackerPosition, request.TargetId, targetPosition, targetActor.IsAlive);
        if (validationResult.IsFail)
        {
            return validationResult.Match(
                Succ: _ => FinSucc(LanguageExt.Unit.Default),
                Fail: error => FinFail<LanguageExt.Unit>(error)
            );
        }

        // Step 5: Apply damage
        var damageResult = await ApplyDamageAsync(request.TargetId, request.CombatAction.BaseDamage, request.CombatAction.Name);
        if (damageResult.IsFail)
        {
            return damageResult;
        }

        // Step 6: Reschedule attacker
        var rescheduleResult = await RescheduleAttackerAsync(request.AttackerId, attackerPosition, request.CombatAction.BaseCost);
        if (rescheduleResult.IsFail)
        {
            return rescheduleResult;
        }

        // Log attack timing information
        _logger?.Information("⏰ Attack completed: {AttackerId} next turn in +{ActionCost}ms",
            request.AttackerId, request.CombatAction.BaseCost.Value);

        // Step 7: Cleanup dead actor
        await CleanupDeadActorAsync(request.TargetId);

        // Step 8: Provide attack success feedback
        if (_attackFeedbackService != null)
        {
            // Check if target died from the attack
            var targetAfterAttack = _actorStateService.GetActor(request.TargetId);
            var wasLethal = targetAfterAttack.Match(
                Some: actor => !actor.IsAlive,
                None: () => true // If not found, assume dead
            );

            // Provide attack success feedback (sequential, not concurrent)
            _attackFeedbackService?.ProcessAttackSuccess(
                request.AttackerId,
                request.TargetId,
                request.CombatAction,
                request.CombatAction.BaseDamage,
                wasLethal);
        }

        return FinSucc(LanguageExt.Unit.Default);
    }

    private Fin<AttackValidation> ValidateAttack(
        ActorId attackerId,
        Position attackerPosition,
        ActorId targetId,
        Position targetPosition,
        bool isTargetAlive)
    {
        _logger?.Debug("Validating attack: {AttackerId} at {AttackerPos} -> {TargetId} at {TargetPos}",
            attackerId, attackerPosition, targetId, targetPosition);

        return AttackValidation.Create(attackerId, attackerPosition, targetId, targetPosition, isTargetAlive);
    }

    private async Task<Fin<LanguageExt.Unit>> ApplyDamageAsync(ActorId targetId, int damage, string attackSource)
    {
        // Get target HP before damage for rich logging
        var targetBeforeOption = _actorStateService.GetActor(targetId);
        var hpBefore = targetBeforeOption.Match(
            Some: actor => actor.Health.Current,
            None: () => 0
        );

        _logger?.Debug("Applying {Damage} damage to {TargetId} from {Source} (HP: {HPBefore})",
            damage, targetId, attackSource, hpBefore);

        // Apply the damage
        var damageCommand = DamageActorCommand.Create(targetId, damage, attackSource);
        var result = await _mediator.Send(damageCommand);

        // Log HP transition with rich formatting
        if (result.IsSucc)
        {
            var targetAfterOption = _actorStateService.GetActor(targetId);
            targetAfterOption.Match(
                Some: actor =>
                {
                    var hpAfter = actor.Health.Current;
                    var maxHp = actor.Health.Maximum;
                    var actualDamage = hpBefore - hpAfter;

                    if (actor.IsAlive)
                    {
                        _logger?.Information("💗 {TargetId} health: {HPBefore} → {HPAfter} ({ActualDamage} damage taken, {HPAfter}/{MaxHP} remaining)",
                            targetId, hpBefore, hpAfter, actualDamage, hpAfter, maxHp);
                    }
                    else
                    {
                        _logger?.Information("💀 {TargetId} defeated: {HPBefore} → 0 HP ({ActualDamage} damage taken, DEAD)",
                            targetId, hpBefore, actualDamage);
                    }
                    return LanguageExt.Unit.Default;
                },
                None: () =>
                {
                    _logger?.Warning("Could not retrieve target {TargetId} after damage application", targetId);
                    return LanguageExt.Unit.Default;
                }
            );
        }

        return result;
    }

    private async Task<Fin<LanguageExt.Unit>> RescheduleAttackerAsync(ActorId attackerId, Position attackerPosition, TimeUnit actionCost)
    {
        _logger?.Debug("Rescheduling {AttackerId} with action cost {ActionCost}",
            attackerId, actionCost);

        // For now, use a simple next turn calculation
        // TODO: Integrate with current game time when time system is implemented
        var nextTurn = TimeUnit.CreateUnsafe(1000 + actionCost.Value); // Current time + action cost

        return await Task.FromResult(_combatSchedulerService.ScheduleActor(attackerId, attackerPosition, nextTurn));
    }

    private async Task<Fin<LanguageExt.Unit>> CleanupDeadActorAsync(ActorId targetId)
    {
        // Check if target died from the attack
        var targetOption = _actorStateService.GetActor(targetId);

        return await Task.FromResult(targetOption.Match(
            Some: actor =>
            {
                if (!actor.IsAlive)
                {
                    _logger?.Information("💀 Target {TargetId} died from attack, performing cleanup", targetId);

                    // Get the target's position before cleanup for notifications
                    var targetPos = _gridStateService.GetActorPosition(targetId);
                    var position = targetPos.Match(p => p, () => new Position(0, 0));

                    // Remove dead actor from combat scheduler
                    var removed = _combatSchedulerService.RemoveActor(targetId);
                    if (removed)
                    {
                        _logger?.Information("✅ Removed dead actor {TargetId} from combat scheduler", targetId);
                    }
                    else
                    {
                        _logger?.Warning("⚠️ Dead actor {TargetId} was not found in combat scheduler", targetId);
                    }

                    // Remove dead actor from grid (frees up the position)
                    _gridStateService.RemoveActorFromGrid(targetId);
                    _logger?.Information("✅ Removed dead actor {TargetId} from grid position", targetId);

                    // Check if callback is wired before invoking
                    if (OnActorDeath != null)
                    {
                        _logger?.Information("📞 Calling death notification callback for {TargetId} at {Position}", targetId, position);
                        OnActorDeath.Invoke(targetId, position);
                        _logger?.Information("✅ Death notification callback completed for {TargetId}", targetId);
                    }
                    else
                    {
                        _logger?.Error("❌ OnActorDeath callback is NULL - UI cleanup will not occur!");
                    }
                }
                return FinSucc(LanguageExt.Unit.Default);
            },
            None: () =>
            {
                _logger?.Warning("Could not verify target state after attack: target not found");
                return FinSucc(LanguageExt.Unit.Default); // Don't fail the whole attack for this
            }
        ));
    }

    private Error MapError(Error error)
    {
        // Map common errors to more specific attack-related messages
        if (error.Message.Contains("not found on grid"))
        {
            return Error.New($"Attack failed: {error.Message}");
        }

        if (error.Message.Contains("not adjacent"))
        {
            return Error.New($"Attack failed: Target is not adjacent for melee attack");
        }

        if (error.Message.Contains("dead"))
        {
            return Error.New($"Attack failed: Cannot attack dead target");
        }

        if (error.Message.Contains("cannot attack itself"))
        {
            return Error.New($"Attack failed: Actor cannot attack itself");
        }

        return error; // Pass through other errors unchanged
    }
}
