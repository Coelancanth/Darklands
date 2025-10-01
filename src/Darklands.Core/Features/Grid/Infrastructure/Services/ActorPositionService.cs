using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Services;

namespace Darklands.Core.Features.Grid.Infrastructure.Services;

/// <summary>
/// In-memory implementation of actor position tracking.
/// Thread-safe for concurrent access (lock-based).
/// </summary>
/// <remarks>
/// For Phase 4 testing with player + dummy actors.
/// Future: Refactor to ECS or database-backed solution for production.
/// </remarks>
public sealed class ActorPositionService : IActorPositionService
{
    private readonly Dictionary<ActorId, Position> _positions = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Result<Position> GetPosition(ActorId actorId)
    {
        lock (_lock)
        {
            if (_positions.TryGetValue(actorId, out var position))
            {
                return Result.Success(position);
            }

            return Result.Failure<Position>(
                $"Actor {actorId} not found in position registry");
        }
    }

    /// <inheritdoc />
    public Result SetPosition(ActorId actorId, Position position)
    {
        lock (_lock)
        {
            // Allow both updates and new registrations
            _positions[actorId] = position;
            return Result.Success();
        }
    }

    /// <inheritdoc />
    public Result<List<ActorId>> GetAllActors()
    {
        lock (_lock)
        {
            return Result.Success(_positions.Keys.ToList());
        }
    }

    /// <summary>
    /// Removes an actor from the position registry.
    /// Used for cleanup (actor death, despawn, etc.).
    /// </summary>
    /// <param name="actorId">Actor to remove</param>
    /// <returns>Success if removed, Failure if actor not found</returns>
    public Result RemoveActor(ActorId actorId)
    {
        lock (_lock)
        {
            if (_positions.Remove(actorId))
            {
                return Result.Success();
            }

            return Result.Failure($"Actor {actorId} not found in position registry");
        }
    }
}
