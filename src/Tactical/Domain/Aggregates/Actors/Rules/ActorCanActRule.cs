using Darklands.SharedKernel.Domain;

namespace Darklands.Tactical.Domain.Aggregates.Actors.Rules;

/// <summary>
/// Business rule ensuring an actor can act (alive and not disabled).
/// </summary>
public sealed class ActorCanActRule : IBusinessRule
{
    private readonly Actor _actor;

    public ActorCanActRule(Actor actor)
    {
        _actor = actor ?? throw new ArgumentNullException(nameof(actor));
    }

    public bool IsSatisfied() => _actor.CanAct;

    public string ErrorMessage
    {
        get
        {
            if (!_actor.IsAlive)
                return $"Actor {_actor.Name} is dead and cannot act";

            if (_actor.IsStunned)
                return $"Actor {_actor.Name} is stunned and cannot act";

            return $"Actor {_actor.Name} cannot act for unknown reasons";
        }
    }
}
