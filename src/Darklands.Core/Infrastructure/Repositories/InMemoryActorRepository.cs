using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Entities;

namespace Darklands.Core.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of IActorRepository.
/// Stores actors in a dictionary for fast O(1) lookups.
/// </summary>
/// <remarks>
/// <para><b>Thread Safety</b>:</para>
/// <para>
/// Not thread-safe - assumes single-threaded game loop (typical for Godot).
/// If multi-threading needed (future), wrap with locks or use ConcurrentDictionary.
/// </para>
///
/// <para><b>Persistence</b>:</para>
/// <para>
/// In-memory only - data lost when game closes.
/// For save/load feature, implement IActorRepository backed by SQLite/JSON.
/// </para>
///
/// <para><b>Performance</b>:</para>
/// <list type="bullet">
/// <item><description>GetByIdAsync: O(1) dictionary lookup</description></item>
/// <item><description>AddActorAsync: O(1) dictionary insert</description></item>
/// <item><description>RemoveActorAsync: O(1) dictionary remove</description></item>
/// <item><description>GetAllAsync: O(n) enumerate all actors</description></item>
/// </list>
/// </remarks>
public class InMemoryActorRepository : IActorRepository
{
    private readonly Dictionary<ActorId, Actor> _actors = new();

    /// <inheritdoc />
    public Task<Result<Actor>> GetByIdAsync(ActorId actorId)
    {
        if (_actors.TryGetValue(actorId, out var actor))
        {
            return Task.FromResult(Result.Success(actor));
        }

        return Task.FromResult(
            Result.Failure<Actor>($"Actor with ID {actorId} not found in repository"));
    }

    /// <inheritdoc />
    public Task<Result> AddActorAsync(Actor actor)
    {
        if (_actors.ContainsKey(actor.Id))
        {
            return Task.FromResult(
                Result.Failure($"Actor with ID {actor.Id} already exists in repository"));
        }

        _actors[actor.Id] = actor;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> RemoveActorAsync(ActorId actorId)
    {
        if (!_actors.Remove(actorId))
        {
            return Task.FromResult(
                Result.Failure($"Actor with ID {actorId} not found in repository"));
        }

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<List<Actor>> GetAllAsync()
    {
        return Task.FromResult(_actors.Values.ToList());
    }
}
