using Darklands.Core.Application.Common;
using Darklands.Core.Domain.Grid;
using LanguageExt;
using MediatR;

namespace Darklands.Core.Application.Actor.Commands
{
    /// <summary>
    /// Command to apply damage to an actor, reducing their current health.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record DamageActorCommand : ICommand, IRequest<Fin<LanguageExt.Unit>>
    {
        /// <summary>
        /// The unique identifier of the actor to damage.
        /// </summary>
        public required ActorId ActorId { get; init; }

        /// <summary>
        /// The amount of damage to apply (must be non-negative).
        /// </summary>
        public required int Damage { get; init; }

        /// <summary>
        /// Optional source of the damage (for logging and combat feedback).
        /// </summary>
        public string? Source { get; init; }

        /// <summary>
        /// Creates a new DamageActorCommand with the specified parameters.
        /// </summary>
        public static DamageActorCommand Create(ActorId actorId, int damage, string? source = null) =>
            new()
            {
                ActorId = actorId,
                Damage = damage,
                Source = source
            };
    }
}
