using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Domain;
using MediatR;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Command to set terrain type at a specific grid position.
/// Used for test map initialization and dynamic terrain changes (e.g., destroying walls).
/// </summary>
/// <param name="Position">Grid position to modify</param>
/// <param name="TerrainType">New terrain type to set</param>
public record SetTerrainCommand(Position Position, TerrainType TerrainType) : IRequest<Result>;
