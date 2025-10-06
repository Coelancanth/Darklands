using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Components;
using Darklands.Core.Features.Combat.Domain;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.Commands;

/// <summary>
/// Handles execution of attacks between actors.
/// Validates range, applies damage, consumes time units, handles death.
/// </summary>
public class ExecuteAttackCommandHandler :
    IRequestHandler<ExecuteAttackCommand, Result<AttackResult>>
{
    private readonly IActorRepository _actors;
    private readonly IActorPositionService _positions;
    private readonly ITurnQueueRepository _turnQueue;
    private readonly IFOVService _fovService;
    private readonly GridMap _gridMap;
    private readonly ILogger<ExecuteAttackCommandHandler> _logger;

    public ExecuteAttackCommandHandler(
        IActorRepository actors,
        IActorPositionService positions,
        ITurnQueueRepository turnQueue,
        IFOVService fovService,
        GridMap gridMap,
        ILogger<ExecuteAttackCommandHandler> logger)
    {
        _actors = actors;
        _positions = positions;
        _turnQueue = turnQueue;
        _fovService = fovService;
        _gridMap = gridMap;
        _logger = logger;
    }

    public async Task<Result<AttackResult>> Handle(
        ExecuteAttackCommand cmd,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Combat: {AttackerId} attacking {TargetId}",
            cmd.AttackerId,
            cmd.TargetId);

        // 1. Get attacker entity
        var attackerResult = await _actors.GetByIdAsync(cmd.AttackerId);
        if (attackerResult.IsFailure)
        {
            return Result.Failure<AttackResult>(
                $"Attacker {cmd.AttackerId} not found");
        }

        var attacker = attackerResult.Value;

        // 2. Validate attacker has weapon
        if (!attacker.HasComponent<IWeaponComponent>())
        {
            return Result.Failure<AttackResult>(
                $"Attacker {cmd.AttackerId} has no weapon equipped");
        }

        var weaponComp = attacker.GetComponent<IWeaponComponent>().Value;
        if (!weaponComp.CanAttack())
        {
            return Result.Failure<AttackResult>(
                $"Attacker {cmd.AttackerId} cannot attack (no weapon)");
        }

        var weapon = weaponComp.EquippedWeapon!;

        // 3. Get target entity
        var targetResult = await _actors.GetByIdAsync(cmd.TargetId);
        if (targetResult.IsFailure)
        {
            return Result.Failure<AttackResult>(
                $"Target {cmd.TargetId} not found");
        }

        var target = targetResult.Value;

        // 4. Validate target has health component
        if (!target.HasComponent<IHealthComponent>())
        {
            return Result.Failure<AttackResult>(
                $"Target {cmd.TargetId} has no health component (cannot be attacked)");
        }

        var targetHealth = target.GetComponent<IHealthComponent>().Value;

        // 5. Validate target is alive
        if (!targetHealth.IsAlive)
        {
            return Result.Failure<AttackResult>(
                $"Target {cmd.TargetId} is already dead");
        }

        // 6. Validate range (melee = adjacent, ranged = line-of-sight)
        var rangeCheck = await ValidateRange(
            cmd.AttackerId,
            cmd.TargetId,
            weapon);

        if (rangeCheck.IsFailure)
        {
            return Result.Failure<AttackResult>(rangeCheck.Error);
        }

        // 7. Apply damage to target
        var damageResult = targetHealth.TakeDamage(weapon.Damage);
        if (damageResult.IsFailure)
        {
            return Result.Failure<AttackResult>(
                $"Failed to apply damage: {damageResult.Error}");
        }

        var newHealth = damageResult.Value;
        var targetDied = !targetHealth.IsAlive;

        _logger.LogInformation(
            "Combat: {AttackerId} dealt {Damage} damage to {TargetId} (remaining: {RemainingHealth})",
            cmd.AttackerId,
            weapon.Damage,
            cmd.TargetId,
            newHealth.Current);

        // 8. Handle death (remove from turn queue AND position service)
        if (targetDied)
        {
            _logger.LogInformation(
                "Combat: {TargetId} defeated",
                cmd.TargetId);

            // Remove from turn queue
            var queue = await _turnQueue.GetAsync(cancellationToken);
            if (queue.IsSuccess && queue.Value.Contains(cmd.TargetId))
            {
                queue.Value.Remove(cmd.TargetId);
                await _turnQueue.SaveAsync(queue.Value, cancellationToken);
            }

            // Remove from position service (visual grid)
            var removeResult = _positions.RemoveActor(cmd.TargetId);
            if (removeResult.IsFailure)
            {
                _logger.LogWarning(
                    "Combat: Failed to remove defeated actor {TargetId} from position service: {Error}",
                    cmd.TargetId,
                    removeResult.Error);
            }
        }

        // 9. Consume time units from attacker (reschedule in turn queue)
        var queueResult = await _turnQueue.GetAsync(cancellationToken);
        if (queueResult.IsSuccess)
        {
            var turnQueue = queueResult.Value;
            var scheduledActor = turnQueue.ScheduledActors
                .FirstOrDefault(a => a.ActorId == cmd.AttackerId);

            if (scheduledActor.ActorId != default)
            {
                var newTime = TimeUnits.Create(
                    scheduledActor.NextActionTime.Value + weapon.TimeCost).Value;

                turnQueue.Reschedule(cmd.AttackerId, newTime);
                await _turnQueue.SaveAsync(turnQueue, cancellationToken);

                _logger.LogInformation(
                    "Combat: {AttackerId} time advanced (cost: {TimeCost}, new time: {NewTime})",
                    cmd.AttackerId,
                    weapon.TimeCost,
                    newTime.Value);
            }
        }

        // 10. Return attack result
        return Result.Success(new AttackResult(
            DamageDealt: weapon.Damage,
            TargetDied: targetDied,
            TargetRemainingHealth: newHealth.Current
        ));
    }

    /// <summary>
    /// Validates attack range based on weapon type.
    /// </summary>
    private Task<Result> ValidateRange(
        ActorId attackerId,
        ActorId targetId,
        Weapon weapon)
    {
        // Get positions
        var attackerPosResult = _positions.GetPosition(attackerId);
        var targetPosResult = _positions.GetPosition(targetId);

        if (attackerPosResult.IsFailure)
        {
            return Task.FromResult(Result.Failure(
                $"Attacker {attackerId} position not found"));
        }

        if (targetPosResult.IsFailure)
        {
            return Task.FromResult(Result.Failure(
                $"Target {targetId} position not found"));
        }

        var attackerPos = attackerPosResult.Value;
        var targetPos = targetPosResult.Value;

        // Calculate distance
        var distance = CalculateDistance(attackerPos, targetPos);

        // Validate based on weapon type
        if (weapon.Type == WeaponType.Melee)
        {
            // Melee: Must be adjacent (distance <= 1, 8-directional)
            if (distance > 1)
            {
                return Task.FromResult(Result.Failure(
                    $"Target out of melee range (distance: {distance}, max: 1)"));
            }
        }
        else // Ranged
        {
            // Ranged: Must be within weapon range
            if (distance > weapon.Range)
            {
                return Task.FromResult(Result.Failure(
                    $"Target out of ranged weapon range (distance: {distance}, max: {weapon.Range})"));
            }

            // Line-of-sight validation (Phase 3): Target must be visible in attacker's FOV
            var fovResult = _fovService.CalculateFOV(_gridMap, attackerPos, weapon.Range);
            if (fovResult.IsFailure)
            {
                return Task.FromResult(Result.Failure(
                    $"Failed to calculate FOV: {fovResult.Error}"));
            }

            var visiblePositions = fovResult.Value;
            if (!visiblePositions.Contains(targetPos))
            {
                return Task.FromResult(Result.Failure(
                    $"Target not visible (line-of-sight blocked by terrain)"));
            }
        }

        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Calculates Chebyshev distance (8-directional) between two positions.
    /// </summary>
    private int CalculateDistance(Position a, Position b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dy = Math.Abs(a.Y - b.Y);
        return Math.Max(dx, dy); // Chebyshev distance (diagonal = 1 move)
    }
}
