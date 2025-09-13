using Darklands.SharedKernel.Domain;

namespace Darklands.Tactical.Domain.Aggregates.Actors.Rules;

/// <summary>
/// Business rule ensuring an actor must be alive to perform actions.
/// </summary>
public sealed class ActorMustBeAliveRule : IBusinessRule
{
    private readonly Actor _actor;

    public ActorMustBeAliveRule(Actor actor)
    {
        _actor = actor ?? throw new ArgumentNullException(nameof(actor));
    }

    public bool IsSatisfied() => _actor.IsAlive;

    public string ErrorMessage => $"Actor {_actor.Name} is dead and cannot perform this action";
}
