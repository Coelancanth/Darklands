using LanguageExt;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Application.Common
{
    /// <summary>
    /// Factory interface for creating and managing game actors.
    /// Provides clean separation between test/initialization logic and production presenters.
    /// Exposes the player ID for other presenters while handling all actor creation internally.
    /// </summary>
    public interface IActorFactory
    {
        /// <summary>
        /// Gets the ID of the current player actor.
        /// Used by other presenters (like GridPresenter) to reference the player.
        /// </summary>
        ActorId? PlayerId { get; }

        /// <summary>
        /// Creates a test player actor at the specified position.
        /// Registers the actor in both the actor state service and grid state service.
        /// </summary>
        /// <param name="position">Grid position where the player should be created</param>
        /// <param name="name">Name for the player character</param>
        /// <returns>Success with ActorId or failure with error details</returns>
        Fin<ActorId> CreatePlayer(Position position, string name = "Test Player");

        /// <summary>
        /// Creates a dummy combat target at the specified position.
        /// Useful for testing combat mechanics and providing practice targets.
        /// </summary>
        /// <param name="position">Grid position where the dummy should be created</param>
        /// <param name="health">Maximum health for the dummy actor</param>
        /// <returns>Success with ActorId or failure with error details</returns>
        Fin<ActorId> CreateDummy(Position position, int health = 50);
    }
}
