using Distributor.Application;

namespace Distributor.Features.PlanDistribution;

public sealed class PlanDistributionResultPresenter : IPresenter<PlanDistributionResult>
{
    private readonly TextWriter _writer;

    public PlanDistributionResultPresenter()
    {
        _writer = Console.Out;
    }

    public void Display(PlanDistributionResult result)
    {
        _writer.WriteLine();
        _writer.WriteLine($"=== Distribution plan {result.DistributionPlanId} ({result.Start} to {result.End}) ===");
        _writer.WriteLine();

        foreach (var period in result.Periods)
        {
            _writer.WriteLine($"--- {period.Date} ---");

            foreach (var shipment in period.Shipments)
            {
                _writer.WriteLine(
                    $"{shipment.Warehouse} → {shipment.Store}: {shipment.Units} units @ ${shipment.UnitCost:F2} = ${shipment.Cost:F2}"
                );
            }

            _writer.WriteLine();
        }

        foreach (var period in result.Periods)
        {
            _writer.WriteLine($"{period.Date} total cost: ${period.TotalCost:F2}");
        }

        _writer.WriteLine();
        _writer.WriteLine($"Total cost: ${result.TotalCost:F2}");
        _writer.WriteLine();
    }
}
