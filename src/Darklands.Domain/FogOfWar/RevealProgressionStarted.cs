using System.Collections.Generic;
using Darklands.Domain.Grid;

namespace Darklands.Domain.FogOfWar
{
    /// <summary>
    /// Domain event raised when an actor begins progressive reveal along a movement path.
    /// This indicates the start of step-by-step FOV revelation.
    /// Pure data structure - MediatR integration happens in Application layer.
    /// </summary>
    public sealed record RevealProgressionStarted(
        ActorId ActorId,
        IReadOnlyList<Position> Path,
        int Turn
    );
}
