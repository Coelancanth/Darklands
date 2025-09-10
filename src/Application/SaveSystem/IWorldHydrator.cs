using LanguageExt;
using Darklands.Core.Domain.GameState;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Application.SaveSystem;

/// <summary>
/// Abstracts the process of reconstructing the Godot scene graph from pure domain state.
/// Maintains Clean Architecture by keeping Godot concerns out of Core/Application layers.
/// 
/// Hydration Process:
/// 1. Clear existing scene nodes
/// 2. Create grid visualization from GridState
/// 3. Spawn actor nodes from Actor entities  
/// 4. Restore positions from saved data
/// 5. Recreate transient state (animations, effects, UI state)
/// 6. Wire up presenters to reconstructed nodes
/// 
/// Implementation lives in Infrastructure/Presentation layers.
/// </summary>
public interface IWorldHydrator
{
    /// <summary>
    /// Reconstructs the entire game world from saved state.
    /// Creates all necessary Godot nodes and binds them to domain data.
    /// </summary>
    /// <param name="state">Complete game state to hydrate</param>
    /// <returns>Success if world rebuilt correctly, or error with failure details</returns>
    Task<Fin<Unit>> HydrateWorldAsync(GameState state);

    /// <summary>
    /// Cleans up the current world state before loading a new save.
    /// Removes all dynamic nodes, preserves UI and system nodes.
    /// </summary>
    /// <returns>Success if cleanup completed, or error with failure details</returns>
    Task<Fin<Unit>> DehydrateWorldAsync();

    /// <summary>
    /// Partially hydrates only actor positions without full world rebuild.
    /// Useful for quick updates during gameplay.
    /// </summary>
    /// <param name="actors">Actors to position in existing world</param>
    /// <returns>Success if actors positioned, or error with failure details</returns>
    Task<Fin<Unit>> HydrateActorPositionsAsync(IReadOnlyDictionary<ActorId, Darklands.Core.Domain.Actor.Actor> actors);

    /// <summary>
    /// Validates that the current world state matches the provided game state.
    /// Used for debugging and integrity checks.
    /// </summary>
    /// <param name="expectedState">Expected game state to validate against</param>
    /// <returns>Success if world matches state, or error with discrepancies</returns>
    Fin<Unit> ValidateWorldConsistency(GameState expectedState);

    /// <summary>
    /// Creates transient state objects for entities that need runtime-only data.
    /// Called after hydration to restore animations, cached data, UI state.
    /// </summary>
    /// <param name="state">Game state containing entities needing transient state</param>
    /// <returns>Success if transient state created, or error with failure details</returns>
    Task<Fin<Unit>> CreateTransientStateAsync(GameState state);
}

/// <summary>
/// Event arguments for world hydration progress updates.
/// Allows UI to show loading progress for large saves.
/// </summary>
public sealed record HydrationProgress(
    HydrationPhase Phase,
    int CompletedItems,
    int TotalItems,
    string CurrentOperation
);

/// <summary>
/// Phases of the world hydration process.
/// </summary>
public enum HydrationPhase
{
    Cleanup,
    GridCreation,
    ActorSpawning,
    PositionRestoration,
    TransientStateCreation,
    PresenterWiring,
    Complete
}
