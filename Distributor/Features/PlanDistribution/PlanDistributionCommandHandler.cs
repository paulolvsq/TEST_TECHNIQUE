using System.Collections.Immutable;
using Distributor.Application;
using Distributor.Data;
using Distributor.Periods;
using Distributor.Plans;
using Distributor.Stores;
using Distributor.Warehouses;
using Microsoft.Extensions.Logging;

namespace Distributor.Features.PlanDistribution;

public sealed class PlanDistributionCommandHandler : IRequestHandler<PlanDistributionCommand, PlanDistributionResult>
{
    private readonly DistributorDatabaseContext _context;
    private readonly IPeriodRepository _periodRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly ITransportSolver _solver;
    private readonly ILogger<PlanDistributionCommandHandler> _logger;

    public PlanDistributionCommandHandler(
        DistributorDatabaseContext context,
        IPeriodRepository periodRepository,
        IWarehouseRepository warehouseRepository,
        IStoreRepository storeRepository,
        ITransportSolver solver,
        ILogger<PlanDistributionCommandHandler> logger
    )
    {
        _context = context;
        _periodRepository = periodRepository;
        _warehouseRepository = warehouseRepository;
        _storeRepository = storeRepository;
        _solver = solver;
        _logger = logger;
    }

    public async Task<PlanDistributionResult> HandleAsync(
        PlanDistributionCommand command,
        CancellationToken token = default
    )
    {
        // TODO: Implement the plan distribution command handler.

        throw new NotImplementedException();
    }
}
