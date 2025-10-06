using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Tests.Features.Combat.Application;

/// <summary>
/// Fake player context for testing.
/// Returns a fixed player ID for log formatting tests.
/// </summary>
public class FakePlayerContext : IPlayerContext
{
    private ActorId _playerId;

    public FakePlayerContext(ActorId playerId)
    {
        _playerId = playerId;
    }

    public void SetPlayerId(ActorId playerId)
    {
        _playerId = playerId;
    }

    public Result<ActorId> GetPlayerId() => Result.Success(_playerId);

    public bool IsPlayer(ActorId actorId) => actorId == _playerId;
}
