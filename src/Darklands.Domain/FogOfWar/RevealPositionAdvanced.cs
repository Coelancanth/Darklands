using Darklands.Domain.Grid;

namespace Darklands.Domain.FogOfWar
{
    /// <summary>
    /// Domain event raised when an actor's reveal position advances to the next step in their path.
    /// This triggers progressive FOV updates as the actor logically moves through the world.
    /// Pure data structure - MediatR integration happens in Application layer.
    /// </summary>
    public sealed record RevealPositionAdvanced(
        ActorId ActorId,
        Position NewRevealPosition,
        Position PreviousPosition,
        int Turn
    );
}
