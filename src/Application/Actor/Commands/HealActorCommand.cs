using Darklands.Core.Application.Common;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Application.Actor.Commands
{
    /// <summary>
    /// Command to apply healing to an actor, restoring their health.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record HealActorCommand : ICommand
    {
        /// <summary>
        /// The unique identifier of the actor to heal.
        /// </summary>
        public required ActorId ActorId { get; init; }

        /// <summary>
        /// The amount of healing to apply (must be non-negative).
        /// </summary>
        public required int HealAmount { get; init; }

        /// <summary>
        /// Optional source of the healing (for logging and combat feedback).
        /// </summary>
        public string? Source { get; init; }

        /// <summary>
        /// Creates a new HealActorCommand with the specified parameters.
        /// </summary>
        public static HealActorCommand Create(ActorId actorId, int healAmount, string? source = null) =>
            new()
            {
                ActorId = actorId,
                HealAmount = healAmount,
                Source = source
            };
    }
}
