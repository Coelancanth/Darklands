using CSharpFunctionalExtensions;
using MediatR;

namespace Darklands.Core.Features.Combat.Application.Queries;

/// <summary>
/// Query to check if currently in combat mode.
/// Used by Presentation layer to route input (exploration vs combat movement).
/// </summary>
/// <remarks>
/// HOT PATH: Called before every movement action.
/// Returns bool directly (not Result) since combat state always exists.
/// </remarks>
public record IsInCombatQuery : IRequest<Result<bool>>;
