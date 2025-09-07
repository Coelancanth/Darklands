using Darklands.Core.Application.Common;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Application.Grid.Commands
{
    /// <summary>
    /// Command to spawn a dummy actor at a specified grid position.
    /// Creates a new DummyActor and places it on the grid for combat testing and training.
    /// </summary>
    public sealed record SpawnDummyCommand : ICommand
    {
        /// <summary>
        /// The grid position where the dummy should be spawned.
        /// </summary>
        public required Position Position { get; init; }

        /// <summary>
        /// The maximum health of the dummy to create.
        /// </summary>
        public required int MaxHealth { get; init; }

        /// <summary>
        /// The name to give the spawned dummy.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Creates a new SpawnDummyCommand with the specified parameters.
        /// </summary>
        /// <param name="position">Grid position for dummy placement</param>
        /// <param name="maxHealth">Maximum health points for the dummy</param>
        /// <param name="name">Name for the dummy</param>
        /// <returns>New SpawnDummyCommand instance</returns>
        public static SpawnDummyCommand Create(Position position, int maxHealth, string name) =>
            new()
            {
                Position = position,
                MaxHealth = maxHealth,
                Name = name
            };

        /// <summary>
        /// Creates a default combat dummy command for quick testing.
        /// </summary>
        /// <param name="position">Grid position for dummy placement</param>
        /// <returns>SpawnDummyCommand for a standard 50HP combat dummy</returns>
        public static SpawnDummyCommand CreateCombatDummy(Position position) =>
            Create(position, 50, "Combat Dummy");

        /// <summary>
        /// Creates a training dummy command with high health.
        /// </summary>
        /// <param name="position">Grid position for dummy placement</param>
        /// <returns>SpawnDummyCommand for a 100HP training dummy</returns>
        public static SpawnDummyCommand CreateTrainingDummy(Position position) =>
            Create(position, 100, "Training Dummy");
    }
}
