using Darklands.Core.Application.Common;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Common;
using System;

namespace Darklands.Core.Application.Vision.Commands
{
    /// <summary>
    /// Console command to calculate and display FOV for testing purposes.
    /// Provides debug output for shadowcasting algorithm verification.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record CalculateFOVConsoleCommand(
        ActorId ViewerId,
        Position Origin,
        VisionRange Range,
        int CurrentTurn,
        bool ShowDebugOutput = false
    ) : ICommand<string>
    {
        /// <summary>
        /// Creates a new CalculateFOVConsoleCommand for testing.
        /// </summary>
        public static CalculateFOVConsoleCommand Create(
            ActorId viewerId,
            Position origin,
            VisionRange range,
            int currentTurn,
            bool showDebug = false) =>
            new(viewerId, origin, range, currentTurn, showDebug);

        /// <summary>
        /// Creates a command with player defaults for quick testing.
        /// Uses a placeholder ActorId for console testing.
        /// </summary>
        public static CalculateFOVConsoleCommand CreatePlayerTest(Position origin, int currentTurn) =>
            new(ActorId.FromGuid(Guid.NewGuid()), origin, VisionRange.Player, currentTurn, true);
    }
}
