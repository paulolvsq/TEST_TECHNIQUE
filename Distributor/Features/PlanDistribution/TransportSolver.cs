using Distributor.Periods;
using Distributor.Plans;
using Highs;

namespace Distributor.Features.PlanDistribution;

public interface ITransportSolver
{
    IReadOnlyList<Shipment> Solve(Period period);
}

public sealed class TransportSolver : ITransportSolver
{
    public IReadOnlyList<Shipment> Solve(Period period)
    {
        var variables = BuildVariables(period);
        var constraints = BuildConstraints(period, variables);
        var model = BuildModel(variables, constraints);

        var solution = Solve(model);

        return BuildShipments(period, variables, solution);
    }

    private static List<TransportVariable> BuildVariables(Period period)
    {
        var variables = new List<TransportVariable>();
        var demandStoreIds = period.Demands.Select(demand => demand.StoreId).ToHashSet();

        foreach (var routeCost in period.Costs)
        {
            if (!demandStoreIds.Contains(routeCost.StoreId))
            {
                continue;
            }

            variables.Add(
                new TransportVariable(routeCost.WarehouseId, routeCost.StoreId, routeCost.UnitCost, variables.Count)
            );
        }

        return variables;
    }

    private static Constraint[] BuildConstraints(Period period, List<TransportVariable> variables)
    {
        var variablesByStoreId = variables.ToLookup(variable => variable.StoreId);
        var variablesByWarehouseId = variables.ToLookup(variable => variable.WarehouseId);

        var demandConstraints = BuildDemandConstraints(period, variablesByStoreId);
        var capacityConstraints = BuildCapacityConstraints(period, variablesByWarehouseId);

        return demandConstraints.Concat(capacityConstraints).ToArray();
    }

    private static List<Constraint> BuildDemandConstraints(
        Period period,
        ILookup<int, TransportVariable> variablesByStoreId
    )
    {
        var constraints = new List<Constraint>();

        foreach (var demand in period.Demands)
        {
            var entries = variablesByStoreId[demand.StoreId].Select(variable => (variable.Index, 1.0)).ToList();

            constraints.Add(new Constraint(demand.Units, demand.Units, entries));
        }

        return constraints;
    }

    private static List<Constraint> BuildCapacityConstraints(
        Period period,
        ILookup<int, TransportVariable> variablesByWarehouseId
    )
    {
        var capacitiesByWarehouse = period.Capacities.ToDictionary(capacity => capacity.WarehouseId);
        var constraints = new List<Constraint>();

        foreach (var group in variablesByWarehouseId)
        {
            var capacity = capacitiesByWarehouse[group.Key];

            var entries = group.Select(variable => (variable.Index, 1.0)).ToList();

            constraints.Add(new Constraint(Lower: 0, Upper: capacity.Units, entries));
        }

        return constraints;
    }

    private static HighsModel BuildModel(List<TransportVariable> variables, Constraint[] constraints)
    {
        var variableCount = variables.Count;
        var variableObjectiveCoefficients = new double[variableCount];
        var variableLowerBounds = new double[variableCount];
        var variableUpperBounds = new double[variableCount];

        foreach (var (variable, index) in variables.Select((variable, index) => (variable, index)))
        {
            variableObjectiveCoefficients[index] = variable.UnitCost;
            variableLowerBounds[index] = 0.0;
            variableUpperBounds[index] = double.PositiveInfinity;
        }

        var constraintLowerBounds = constraints.Select(constraint => constraint.Lower).ToArray();
        var constraintUpperBounds = constraints.Select(constraint => constraint.Upper).ToArray();
        var constraintsSparseMatrix = BuildConstraintsSparseMatrix(constraints);

        int[]? integrality = null;
        const double offset = 0;
        const HighsMatrixFormat format = HighsMatrixFormat.kRowwise;
        const HighsObjectiveSense objectiveSense = HighsObjectiveSense.kMinimize;

        return new HighsModel(
            variableObjectiveCoefficients,
            variableLowerBounds,
            variableUpperBounds,
            constraintLowerBounds,
            constraintUpperBounds,
            constraintsSparseMatrix.RowPointers,
            constraintsSparseMatrix.ColumnIndexes,
            constraintsSparseMatrix.Values,
            integrality,
            offset,
            format,
            objectiveSense
        );
    }

    private static CompressedSparseMatrix BuildConstraintsSparseMatrix(Constraint[] constraints)
    {
        var constraintCount = constraints.Length;
        var rowPointers = new int[constraintCount + 1];
        var nonzeroEntries = new List<(int ColumnIndex, double Value)>();

        for (var constraintIndex = 0; constraintIndex < constraintCount; constraintIndex++)
        {
            var constraint = constraints[constraintIndex];
            rowPointers[constraintIndex] = nonzeroEntries.Count;

            foreach (var (columnIndex, value) in constraint.Entries)
            {
                nonzeroEntries.Add((columnIndex, value));
            }
        }

        rowPointers[constraintCount] = nonzeroEntries.Count;

        var columnIndices = nonzeroEntries.Select(entry => entry.ColumnIndex).ToArray();
        var values = nonzeroEntries.Select(entry => entry.Value).ToArray();

        return new CompressedSparseMatrix(rowPointers, columnIndices, values);
    }

    private static HighsSolution Solve(HighsModel model)
    {
        using var solver = new HighsLpSolver();

        solver.setStringOptionValue("output_flag", "false");

        var passStatus = solver.passLp(model);

        if (passStatus is not HighsStatus.kOk)
        {
            throw new InvalidOperationException($"Highs solver failed to accept the model: {passStatus}.");
        }

        var runStatus = solver.run();

        if (runStatus is not HighsStatus.kOk)
        {
            throw new InvalidOperationException($"Highs solver failed: {runStatus}.");
        }

        var modelStatus = solver.GetModelStatus();

        if (modelStatus is not HighsModelStatus.kOptimal)
        {
            throw new InvalidOperationException(
                $"Highs solver did not find an optimal solution. Model status: {modelStatus}."
            );
        }

        return solver.getSolution();
    }

    private static List<Shipment> BuildShipments(
        Period period,
        IEnumerable<TransportVariable> variables,
        HighsSolution solution
    )
    {
        var shipments = new List<Shipment>();

        foreach (var variable in variables)
        {
            var units = (int)Math.Round(solution.colvalue[variable.Index], MidpointRounding.AwayFromZero);

            if (units <= 0)
            {
                continue;
            }

            shipments.Add(new Shipment(period.Id, variable.WarehouseId, variable.StoreId, units));
        }

        return shipments;
    }

    private sealed record TransportVariable(int WarehouseId, int StoreId, double UnitCost, int Index);

    private sealed record Constraint(double Lower, double Upper, List<(int ColumnIndex, double Value)> Entries);

    private sealed record CompressedSparseMatrix(int[] RowPointers, int[] ColumnIndexes, double[] Values);
}
