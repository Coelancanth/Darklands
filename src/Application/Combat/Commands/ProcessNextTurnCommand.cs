using System;
using LanguageExt;
using Darklands.Application.Common;

namespace Darklands.Application.Combat.Commands
{
    /// <summary>
    /// Command to process the next turn in the combat timeline.
    /// Removes and returns the next actor scheduled to act.
    /// 
    /// Returns Option&lt;Guid&gt; where:
    /// - Some(guid): The Id of the next actor to act
    /// - None: No actors are scheduled (empty timeline)
    /// 
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public sealed record ProcessNextTurnCommand : ICommand<Option<Guid>>
    {
        /// <summary>
        /// Creates a new ProcessNextTurnCommand
        /// </summary>
        public static ProcessNextTurnCommand Create() => new();
    }
}
