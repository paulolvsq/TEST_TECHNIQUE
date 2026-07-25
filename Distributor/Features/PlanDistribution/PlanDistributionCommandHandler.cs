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
        _logger.LogInformation("Generating distribution plan from {Start} to {End}.", command.Start, command.End);

        // 1. Charger les périodes dans la plage de dates demandée
        var periods = await _periodRepository
            .GetPeriodsAsync(command.Start, command.End, token)
            .ConfigureAwait(false);

        if (periods.Count is 0)
        {
            throw new InvalidOperationException("No periods found in the specified range.");
        }

        // 2. Extraire les ID uniques et charger les entrepôts/magasins référencés
        var warehouseIds = periods.SelectMany(p => p.Costs).Select(c => c.WarehouseId).Distinct();
        var storeIds = periods.SelectMany(p => p.Costs).Select(c => c.StoreId).Distinct();

        var warehousesList = await _warehouseRepository.GetWarehousesAsync(warehouseIds, token).ConfigureAwait(false);
        var storesList = await _storeRepository.GetStoresAsync(storeIds, token).ConfigureAwait(false);

        // Indexation pour un accès O(1) aux noms requis pour ShipmentResult
        var warehouseNames = warehousesList.ToDictionary(w => w.Id, w => w.Name);
        var storeNames = storesList.ToDictionary(s => s.Id, s => s.Name);

        var allShipments = new List<Shipment>();
        var periodResults = new List<PeriodResult>(periods.Count);
        var totalPlanCost = 0m;

        // 3. Résoudre l'optimisation pour chaque période
        foreach (var period in periods)
        {
            // Le solveur nous retourne directement les entités Shipment
            var solvedShipments = _solver.Solve(period);
            var shipmentResults = new List<ShipmentResult>(solvedShipments.Count);
            var periodTotalCost = 0m; // Valeur 0 explicite

            // Indexation des coûts unitaires de la période pour un accès O(1)
            var routeCosts = period.Costs.ToDictionary(
                c => (c.WarehouseId, c.StoreId),
                c => (decimal)c.UnitCost // Cast en decimal comme demandé dans les spécifications
            );

            foreach (var shipment in solvedShipments)
            {
                if (!routeCosts.TryGetValue((shipment.WarehouseId, shipment.StoreId), out var unitCost))
                {
                    throw new InvalidOperationException($"Missing route cost for Warehouse {shipment.WarehouseId} to Store {shipment.StoreId}.");
                }

                // Calculs avec le type decimal
                var cost = shipment.Units * unitCost;
                periodTotalCost += cost;

                // Récupération des noms
                var warehouseName = warehouseNames.GetValueOrDefault(shipment.WarehouseId) ?? shipment.WarehouseId.ToString();
                var storeName = storeNames.GetValueOrDefault(shipment.StoreId) ?? shipment.StoreId.ToString();

                // DTO pour le résultat
                shipmentResults.Add(new ShipmentResult
                {
                    Warehouse = warehouseName,
                    Store = storeName,
                    Units = shipment.Units,
                    UnitCost = unitCost,
                    Cost = cost
                });

                // Accumulation des entités Shipment pour la base de données
                allShipments.Add(shipment);
            }

            totalPlanCost += periodTotalCost;
            
            periodResults.Add(new PeriodResult
            {
                Date = new PeriodDate(period.Year, period.Month),
                TotalCost = periodTotalCost,
                Shipments = shipmentResults.ToImmutableArray()
            });
        }

        // 4. Persister l'entité DistributionPlan
        var planName = $"Distribution Plan {command.Start.Year}-{command.Start.Month} to {command.End.Year}-{command.End.Month}";
        
        var plan = new DistributionPlan(
            planName,
            DateTime.UtcNow,
            periods[0].Id, // Identifiant de la première période
            periods[^1].Id, // Identifiant de la dernière période
            allShipments
        );

        _context.DistributionPlans.Add(plan);
        await _context.SaveChangesAsync(token).ConfigureAwait(false);

        _logger.LogInformation("Successfully saved DistributionPlan {PlanId} with total cost {Cost}.", plan.Id, totalPlanCost);

        // 5. Retourner le résultat global
        return new PlanDistributionResult
        {
            DistributionPlanId = plan.Id, // L'ID est généré après SaveChangesAsync
            Start = command.Start,
            End = command.End,
            TotalCost = totalPlanCost,
            Periods = periodResults.ToImmutableArray()
        };
    }

}
