using MediatR;
using Darklands.Domain.Grid;

namespace Darklands.Domain.Combat;

/// <summary>
/// Domain event published when an actor dies during combat.
/// Replaces static callback anti-pattern with proper MediatR notification.
/// 
/// Used for:
/// - Visual cleanup (removing sprites, health bars)
/// - Game state updates (removing from scheduler, grid)
/// - Statistics tracking
/// </summary>
/// <param name="ActorId">The ID of the actor that died</param>
/// <param name="Position">The position where the actor died</param>
public sealed record ActorDiedEvent(
    ActorId ActorId,
    Position Position
) : INotification
{
    /// <summary>
    /// Creates an ActorDiedEvent for the specified actor at the given position.
    /// </summary>
    /// <param name="actorId">The actor that died</param>
    /// <param name="position">Where the actor died</param>
    /// <returns>A new ActorDiedEvent</returns>
    public static ActorDiedEvent Create(ActorId actorId, Position position) =>
        new(actorId, position);

    public override string ToString() =>
        $"ActorDiedEvent(ActorId: {ActorId}, Position: {Position})";
}
