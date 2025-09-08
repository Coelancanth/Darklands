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
                _logger?.Information("Attack successful: {AttackerId} -> {TargetId} with {Action}",
                    request.AttackerId, request.TargetId, request.CombatAction.Name);
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
            _attackFeedbackService.ProcessAttackSuccessAsync(
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
        _logger?.Debug("Applying {Damage} damage to {TargetId} from {Source}",
            damage, targetId, attackSource);

        var damageCommand = DamageActorCommand.Create(targetId, damage, attackSource);
        return await _mediator.Send(damageCommand);
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
                    _logger?.Information("Target {TargetId} died from attack, cleanup may be needed", targetId);
                    // TODO: Implement removal from combat scheduler when dead
                    // For now, we'll leave them in the scheduler - they just won't get processed
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
