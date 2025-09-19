using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Application.Grid.Services;

namespace Darklands.Application.Grid.Queries
{
    /// <summary>
    /// Handler for GetGridStateQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Provides read-only access to current grid state for UI presentation.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class GetGridStateQueryHandler : IRequestHandler<GetGridStateQuery, Fin<Domain.Grid.Grid>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly ICategoryLogger _logger;

        public GetGridStateQueryHandler(
            IGridStateService gridStateService,
            ICategoryLogger logger)
        {
            _gridStateService = gridStateService;
            _logger = logger;
        }

        public Task<Fin<Domain.Grid.Grid>> Handle(GetGridStateQuery request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Command, "Processing GetGridStateQuery");

            var gridResult = _gridStateService.GetCurrentGrid();
            var result = gridResult.Match(
                Succ: grid =>
                {
                    _logger.Log(LogLevel.Debug, LogCategory.Command, "Retrieved grid state successfully");
                    return gridResult;
                },
                Fail: error =>
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Command, "Failed to retrieve grid state: {Error}", error.Message);
                    return gridResult;
                }
            );

            return Task.FromResult(result);
        }
    }
}
