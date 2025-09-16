using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Application.Grid.Services;
using static LanguageExt.Prelude;

namespace Darklands.Application.Grid.Queries
{
    /// <summary>
    /// Handler for ValidateMovementQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Provides validation for movement legality without modifying state.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class ValidateMovementQueryHandler : IRequestHandler<ValidateMovementQuery, Fin<bool>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly ICategoryLogger _logger;

        public ValidateMovementQueryHandler(
            IGridStateService gridStateService,
            ICategoryLogger logger)
        {
            _gridStateService = gridStateService;
            _logger = logger;
        }

        public Task<Fin<bool>> Handle(ValidateMovementQuery request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Processing ValidateMovementQuery from {FromPosition} to {ToPosition}",
                request.FromPosition, request.ToPosition);

            var validationResult = _gridStateService.ValidateMove(request.FromPosition, request.ToPosition);
            var result = validationResult.Match(
                Succ: _ =>
                {
                    _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Movement validation successful");
                    return FinSucc(true);
                },
                Fail: error =>
                {
                    _logger.Log(LogLevel.Debug, LogCategory.Gameplay, "Movement validation failed: {Error}", error.Message);
                    return FinSucc(false);  // Return false rather than error for validation query
                }
            );

            return Task.FromResult(result);
        }
    }
}
