using LanguageExt;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Contracts;
using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Application.Features.Combat.Attack;
using Darklands.Tactical.Application.Features.Combat.Scheduling;
using Darklands.Tactical.Domain.ValueObjects;
using Unit = LanguageExt.Unit;

namespace Darklands.Tactical.Infrastructure.Adapters;

/// <summary>
/// TEMPORARY adapter for Strangler Fig migration (TD_043).
/// Wraps the new Tactical handlers and publishes contract events
/// to enable parallel operation between old and new combat systems.
/// Will be removed in TD_045 when migration is complete.
/// </summary>
public sealed class TacticalContractAdapter
{
    private readonly ExecuteAttackCommandHandler _attackHandler;
    private readonly ProcessNextTurnCommandHandler _turnHandler;
    private readonly IActorRepository _actorRepository;
    private readonly ICombatSchedulerService _schedulerService;
    private readonly IPublisher _publisher;

    public TacticalContractAdapter(
        ExecuteAttackCommandHandler attackHandler,
        ProcessNextTurnCommandHandler turnHandler,
        IActorRepository actorRepository,
        ICombatSchedulerService schedulerService,
        IPublisher publisher)
    {
        _attackHandler = attackHandler;
        _turnHandler = turnHandler;
        _actorRepository = actorRepository;
        _schedulerService = schedulerService;
        _publisher = publisher;
    }

    /// <summary>
    /// Executes attack in new Tactical context AND publishes contract event for comparison.
    /// </summary>
    public async Task<Fin<AttackResult>> ExecuteAttackWithContractAsync(ExecuteAttackCommand request, CancellationToken cancellationToken = default)
    {
        // Get initial state for comparison
        var targetBeforeResult = await _actorRepository.GetByIdAsync(request.TargetId);
        var healthBefore = targetBeforeResult.Match(
            Succ: actor => actor.Health,
            Fail: _ => 0
        );

        // Execute the attack using new Tactical handler
        var result = await _attackHandler.Handle(request, cancellationToken);

        // If successful, publish contract event for parallel validation
        if (result.IsSucc)
        {
            var attackResult = result.Match(
                Succ: r => r,
                Fail: _ => new AttackResult(0, 0, false, new List<string>())
            );

            // Get post-attack state
            var targetAfterResult = await _actorRepository.GetByIdAsync(request.TargetId);
            var targetDefeated = attackResult.TargetKilled;

            // Publish contract event for cross-context communication
            var contractEvent = AttackExecutedEvent.Create(
                attackerId: request.AttackerId,
                targetId: request.TargetId,
                actionName: request.AttackType ?? "Basic Attack",
                damage: attackResult.DamageDealt,
                timeCost: 100, // Default time cost since it's not in the command
                targetDefeated: targetDefeated
            );

            // Publish asynchronously (fire-and-forget for performance)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _publisher.Publish(contractEvent, cancellationToken);
                }
                catch
                {
                    // Swallow exceptions to avoid breaking tactical operations
                    // Contract events are for monitoring/validation, not critical path
                }
            });
        }

        return result;
    }

    /// <summary>
    /// Processes turn in new Tactical context AND publishes contract event for comparison.
    /// </summary>
    public async Task<Fin<TurnResult>> ProcessTurnWithContractAsync(ProcessNextTurnCommand request, CancellationToken cancellationToken = default)
    {
        // Execute the turn processing using new Tactical handler
        var result = await _turnHandler.Handle(request, cancellationToken);

        // If successful, publish contract event for parallel validation
        if (result.IsSucc)
        {
            // Get current scheduler state
            var scheduleResult = await _schedulerService.GetScheduleAsync();
            
            scheduleResult.Match(
                Succ: schedule =>
                {
                    if (schedule.IsEmpty) return Unit.Default;

                    var currentActor = schedule.First();
                    var nextActor = schedule.Count > 1 ? schedule[1] : currentActor;

                    // Publish contract event for cross-context communication
                    var contractEvent = TurnProcessedEvent.Create(
                        actorId: currentActor.ActorId,
                        currentTime: request.CurrentTime.Value,
                        nextTurnTime: nextActor.ActionTime.Value,
                        actorsRemaining: schedule.Count
                    );

                    // Publish asynchronously (fire-and-forget for performance)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _publisher.Publish(contractEvent, cancellationToken);
                        }
                        catch
                        {
                            // Swallow exceptions to avoid breaking tactical operations
                        }
                    });

                    return Unit.Default;
                },
                Fail: _ => Unit.Default // Continue even if we can't get state
            );
        }

        return result;
    }
}