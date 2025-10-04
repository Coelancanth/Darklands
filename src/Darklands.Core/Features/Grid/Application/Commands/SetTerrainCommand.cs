using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Command to set terrain at a specific grid position by name.
/// Used for test map initialization and dynamic terrain changes (e.g., destroying walls).
/// </summary>
/// <param name="Position">Grid position to modify</param>
/// <param name="TerrainName">Terrain name to set (e.g., "floor", "wall", "smoke")</param>
/// <remarks>
/// ARCHITECTURE CHANGE (VS_019 Phase 1):
/// - OLD: Took TerrainType enum (hardcoded in caller)
/// - NEW: Takes terrain name string (resolved via ITerrainRepository in handler)
/// - Presentation layer doesn't need TerrainDefinition - just uses names
/// </remarks>
public record SetTerrainCommand(Position Position, string TerrainName) : IRequest<Result>;
