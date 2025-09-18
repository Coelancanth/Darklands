using Darklands.Domain.Grid;

namespace Darklands.Domain.FogOfWar
{
    /// <summary>
    /// Domain event raised when an actor completes their reveal progression and reaches their final destination.
    /// This indicates the end of step-by-step FOV revelation and return to normal game state.
    /// Pure data structure - MediatR integration happens in Application layer.
    /// </summary>
    public sealed record RevealProgressionCompleted(
        ActorId ActorId,
        Position FinalPosition,
        int Turn
    );
}
