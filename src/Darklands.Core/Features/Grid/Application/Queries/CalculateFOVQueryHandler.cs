using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Queries;

/// <summary>
/// Handler for CalculateFOVQuery.
/// Validates inputs and delegates FOV calculation to IFOVService.
/// </summary>
public class CalculateFOVQueryHandler : IRequestHandler<CalculateFOVQuery, Result<HashSet<Position>>>
{
    private readonly GridMap _gridMap;
    private readonly IFOVService _fovService;
    private readonly ILogger<CalculateFOVQueryHandler> _logger;

    public CalculateFOVQueryHandler(
        GridMap gridMap,
        IFOVService fovService,
        ILogger<CalculateFOVQueryHandler> logger)
    {
        _gridMap = gridMap;
        _fovService = fovService;
        _logger = logger;
    }

    public Task<Result<HashSet<Position>>> Handle(CalculateFOVQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Calculating FOV from position ({X}, {Y}) with radius {Radius}",
            request.Observer.X,
            request.Observer.Y,
            request.Radius);

        // Validate radius
        if (request.Radius <= 0)
        {
            _logger.LogWarning("FOV calculation failed: Invalid radius {Radius}", request.Radius);
            return Task.FromResult(Result.Failure<HashSet<Position>>(
                $"Vision radius must be positive (got {request.Radius})"));
        }

        // Validate observer position is in bounds
        if (!_gridMap.IsValidPosition(request.Observer))
        {
            _logger.LogWarning(
                "FOV calculation failed: Observer position ({X}, {Y}) is out of bounds",
                request.Observer.X,
                request.Observer.Y);
            return Task.FromResult(Result.Failure<HashSet<Position>>(
                $"Observer position ({request.Observer.X}, {request.Observer.Y}) is outside grid bounds"));
        }

        // Delegate to FOV service
        var result = _fovService.CalculateFOV(_gridMap, request.Observer, request.Radius);

        if (result.IsSuccess)
        {
            _logger.LogDebug(
                "FOV calculated successfully: {VisibleCount} positions visible",
                result.Value.Count);
        }

        return Task.FromResult(result);
    }
}
