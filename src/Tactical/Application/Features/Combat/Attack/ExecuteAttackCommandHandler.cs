using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Domain.Aggregates.Actors;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Application.Features.Combat.Attack;

/// <summary>
/// Handles the execution of attack commands in the tactical combat system.
/// </summary>
public sealed class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Fin<AttackResult>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMediator _mediator;

    public ExecuteAttackCommandHandler(
        IActorRepository actorRepository,
        IMediator mediator)
    {
        _actorRepository = actorRepository;
        _mediator = mediator;
    }

    public async Task<Fin<AttackResult>> Handle(ExecuteAttackCommand command, CancellationToken cancellationToken)
    {
        // Execute attack from attacker to target

        // Retrieve actors
        var getAttacker = await _actorRepository.GetByIdAsync(command.AttackerId);
        var getTarget = await _actorRepository.GetByIdAsync(command.TargetId);

        // Validate both actors exist
        var validation = from attacker in getAttacker
                         from target in getTarget
                         select (attacker, target);

        if (validation.IsFail)
            return validation.Match<Fin<AttackResult>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: err => FinFail<AttackResult>(err)
            );

        var actors = validation.Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException()
        );

        return await ExecuteAttack(actors.attacker, actors.target, command);
    }

    private async Task<Fin<AttackResult>> ExecuteAttack(
        Actor attacker,
        Actor target,
        ExecuteAttackCommand command)
    {
        // Validate attacker can act
        if (!attacker.CanAct)
        {
            return FinFail<AttackResult>(Error.New($"Attacker {attacker.Name} cannot act"));
        }

        // Apply damage to target
        var damageResult = target.TakeDamage(command.BaseDamage, command.OccurredAt);

        if (damageResult.IsFail)
            return damageResult.Match<Fin<AttackResult>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: err => FinFail<AttackResult>(err)
            );

        var actualDamage = damageResult.Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException()
        );

        // Save the updated target state
        await _actorRepository.UpdateAsync(target);

        // Publish domain events as integration events
        foreach (var domainEvent in target.DomainEvents)
        {
            await PublishDomainEvent(domainEvent, cancellationToken: default);
        }

        // Clear domain events after publishing
        target.ClearDomainEvents();

        var effects = new List<string>();
        if (target.Health == 0)
        {
            effects.Add($"{target.Name} was killed!");
        }

        return FinSucc(new AttackResult(
            actualDamage,
            target.Health,
            target.Health == 0,
            effects
        ));
    }

    private async Task PublishDomainEvent(object domainEvent, CancellationToken cancellationToken)
    {
        // This will be enhanced in Phase 3 to publish contract events
        // For now, just log the domain event
        // Domain event occurred

        // TODO: In Phase 3, add TacticalContractAdapter to convert domain events to contract events
        await Task.CompletedTask;
    }
}
