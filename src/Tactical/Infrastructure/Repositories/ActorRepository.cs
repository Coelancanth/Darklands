using System.Collections.Concurrent;
using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Domain.Aggregates.Actors;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the Actor repository.
/// Thread-safe using ConcurrentDictionary for parallel access.
/// </summary>
public sealed class ActorRepository : IActorRepository
{
    private readonly ConcurrentDictionary<EntityId, Actor> _actors = new();

    /// <inheritdoc />
    public Task<Fin<Actor>> GetByIdAsync(EntityId id)
    {
        if (_actors.TryGetValue(id, out var actor))
        {
            return Task.FromResult(FinSucc(actor));
        }

        return Task.FromResult(
            FinFail<Actor>(Error.New(404, $"Actor with ID {id.Value} not found"))
        );
    }

    /// <inheritdoc />
    public Task<Fin<Seq<Actor>>> GetAllAsync()
    {
        var actors = toSeq(_actors.Values);
        return Task.FromResult(FinSucc(actors));
    }

    /// <inheritdoc />
    public Task<Fin<Unit>> AddAsync(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (_actors.TryAdd(actor.Id, actor))
        {
            return Task.FromResult(FinSucc(unit));
        }

        return Task.FromResult(
            FinFail<Unit>(Error.New(409, $"Actor with ID {actor.Id.Value} already exists"))
        );
    }

    /// <inheritdoc />
    public Task<Fin<Unit>> UpdateAsync(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (_actors.TryUpdate(actor.Id, actor, _actors.GetValueOrDefault(actor.Id)!))
        {
            return Task.FromResult(FinSucc(unit));
        }

        return Task.FromResult(
            FinFail<Unit>(Error.New(404, $"Actor with ID {actor.Id.Value} not found"))
        );
    }

    /// <inheritdoc />
    public Task<Fin<Unit>> RemoveAsync(EntityId id)
    {
        if (_actors.TryRemove(id, out _))
        {
            return Task.FromResult(FinSucc(unit));
        }

        return Task.FromResult(
            FinFail<Unit>(Error.New(404, $"Actor with ID {id.Value} not found"))
        );
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(EntityId id)
    {
        return Task.FromResult(_actors.ContainsKey(id));
    }

    /// <summary>
    /// Clears all actors from the repository.
    /// Useful for testing and resetting state.
    /// </summary>
    public void Clear()
    {
        _actors.Clear();
    }
}