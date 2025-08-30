using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Grid.Services;

namespace Darklands.Core.Application.Grid.Queries
{
    /// <summary>
    /// Handler for GetGridStateQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Provides read-only access to current grid state for UI presentation.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class GetGridStateQueryHandler : IRequestHandler<GetGridStateQuery, Fin<Domain.Grid.Grid>>
    {
        private readonly IGridStateService _gridStateService;
        private readonly ILogger _logger;

        public GetGridStateQueryHandler(
            IGridStateService gridStateService,
            ILogger logger)
        {
            _gridStateService = gridStateService;
            _logger = logger;
        }

        public Task<Fin<Domain.Grid.Grid>> Handle(GetGridStateQuery request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing GetGridStateQuery");

            var gridResult = _gridStateService.GetCurrentGrid();
            var result = gridResult.Match(
                Succ: grid =>
                {
                    _logger?.Debug("Retrieved grid state successfully");
                    return gridResult;
                },
                Fail: error =>
                {
                    _logger?.Warning("Failed to retrieve grid state: {Error}", error.Message);
                    return gridResult;
                }
            );

            return Task.FromResult(result);
        }
    }
}
