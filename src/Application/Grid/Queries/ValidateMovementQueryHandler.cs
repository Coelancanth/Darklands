using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Services;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Grid.Queries
{
    /// <summary>
    /// Handler for ValidateMovementQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Provides validation for movement legality without modifying state.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class ValidateMovementQueryHandler : IRequestHandler<ValidateMovementQuery, Fin<bool>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly ILogger _logger;

        public ValidateMovementQueryHandler(
            IGridStateService gridStateService,
            ILogger logger)
        {
            _gridStateService = gridStateService;
            _logger = logger;
        }

        public Task<Fin<bool>> Handle(ValidateMovementQuery request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing ValidateMovementQuery from {FromPosition} to {ToPosition}",
                request.FromPosition, request.ToPosition);

            var validationResult = _gridStateService.ValidateMove(request.FromPosition, request.ToPosition);
            var result = validationResult.Match(
                Succ: _ =>
                {
                    _logger?.Debug("Movement validation successful");
                    return FinSucc(true);
                },
                Fail: error =>
                {
                    _logger?.Debug("Movement validation failed: {Error}", error.Message);
                    return FinSucc(false);  // Return false rather than error for validation query
                }
            );

            return Task.FromResult(result);
        }
    }
}
