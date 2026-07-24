# Distributor

A distribution network cost analysis and planning tool. Warehouses ship goods to stores across
monthly periods, each with its own warehouse capacities, store demands, and route costs.

## Application overview

.NET 10 console application with two features:

1. **Evaluate scenarios**: A query feature that evaluates maximum distribution cost under pricing
   scenarios over a given period range. Scenarios define hypothetical cost multipliers at warehouse
   and store level. A base scenario (no adjustments) is always included for comparison. The feature
   uses parallelized matrix multiplication to compute costs across all warehouse-store-scenario
   combinations.

2. **Plan distribution**: A command feature that plans optimal shipment allocation over a given
   period range using linear programming (HiGHS solver) and saves the plan in the database. For
   each period, determines the cost-minimizing distribution of goods from warehouses to stores,
   respecting capacity and demand constraints.

Key dependencies:

- `Microsoft.EntityFrameworkCore.Sqlite`: Entity Framework Core provider for SQLite
- `System.CommandLine`: command-line interface framework
- `MathNet.Numerics`: matrix multiplication
- `Highs.Native`: .NET interface for HiGHS linear optimization solver

## Domain

The distribution network is made up of **warehouses** and **stores** connected by **routes**. If a
warehouse and a store are connected by a route, the warehouse can ship goods to that store.

A **period** represents one month, such as `2026-01`. During each period, each warehouse has a
**capacity** (maximum units it can ship), each store has a **demand** (total units it needs to
receive), and each route has a **unit cost** (cost per unit shipped along that route).

A **scenario** models a hypothetical situation by applying multipliers to warehouses
and stores. A warehouse adjustment scales that warehouse's cost contribution. A store adjustment
scales that store's demand. Scenarios are compared against a base scenario (with no adjustments) to
evaluate the cost impact of different possible conditions.

A **distribution plan** for a given period range consists of a set of **shipments** assigning
specific unit quantities from warehouses to stores for each period, minimizing total cost while
respecting capacity and demand constraints.

## Getting started

### 1. .NET SDK

Download and install [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

### 2. Seed database

Seed the SQLite database file by running the seeder using the `seed` or `reset` command.
You must seed the database before running the application.

```
dotnet run --project Distributor.Seeder -- seed --small
dotnet run --project Distributor.Seeder -- reset --large
```

Run either command with `--help` to view usage details.

You can also use launch profiles in `Distributor.Seeder/Properties/launchSettings.json` to run the
application directly from your IDE.

The database file is stored at:

- Windows: `%LOCALAPPDATA%/Distributor/distributor.db`
- Linux / macOS: `~/.local/share/Distributor/distributor.db`

### 3. Run application

Evaluate scenarios:

```
dotnet run --project Distributor -- evaluate --start 2026-01 --end 2026-03 --scenarios 1 3 5
```

Plan distribution:

```
dotnet run --project Distributor -- plan --start 2026-01 --end 2026-03
```

Run either command with `--help` to view usage details.

You can also use launch profiles in `Distributor/Properties/launchSettings.json` to run the
application directly from your IDE.

### 4. Test application

```
dotnet test
```

## Exercise instructions

Budget approximately 6 hours total.

### Part 1: Debug the evaluate scenarios feature

The `evaluate` command contains bugs across three areas:

- **Concurrency bugs** in `MatrixMultiplier` and `MatrixSpanMultiplier`. The matrix
  multiplication is intended to split input matrices into sub-matrices (spans) and multiply
  corresponding spans in parallel to fill the result matrix. The current implementation has
  bugs that prevent correct parallel execution.

- **Matrix construction bugs** in the scenario matrix factory
  (`Distributor/Features/EvaluateScenarios/ScenarioMatrixFactory.cs`). The factory builds the
  cost matrix and multiplier matrices used in the evaluation. Review matrix dimensions and
  default values.

- **Domain logic bugs** in the query handler
  (`Distributor/Features/EvaluateScenarios/EvaluateScenariosQueryHandler.cs`). The handler
  orchestrates period loading, matrix construction, and result assembly. Review how data flows
  across periods.

Find and fix all bugs. The feature should always produce correct results for any given query.
The matrix multiplication should multiply matrix spans (sub-matrices) in parallel.

Any bugs you discover must have tests that demonstrate and protect against those bugs. Such tests
might already exist. Otherwise, write them yourself.

### Part 2: Implement the plan distribution feature

The `plan` command handler (`PlanDistributionCommandHandler.HandleAsync`) is not implemented.
The method signature, class, constructor, and all dependencies are in place.

Implement it according to the following specification:

1. Load periods in the requested date range from the database. Throw if none are found.
2. Load warehouses and stores referenced by those periods.
3. For each period, call `ITransportSolver.Solve(period)` to compute optimal shipments.
4. Build result DTOs for each period. A `PeriodResult` contains the period's date, total cost,
   and an array of `ShipmentResult` entries. Each shipment's cost is `units * unitCost` where
   the unit cost comes from the period's route costs for that warehouse-store pair.
   Use `decimal` for all cost values.
5. Persist a `DistributionPlan` entity (with all shipments across all periods) to the database
   via `DistributorDatabaseContext`.
6. Return a `PlanDistributionResult` with the plan id, date range, per-period breakdowns, and
   total cost.

Refer to the domain entities (`Period`, `DistributionPlan`, `Shipment`), the repositories, and
the evaluate scenarios feature handler (once debugged) for patterns and conventions used in
the codebase.

### Part 3: Improve code quality

You have a green light to improve the codebase in whatever way you see fit. Priority is correct
functionality, but then do what you can to improve maintainability, readability, performance,
and other non-functional qualities without breaking functionality or causing tests to fail.

## Evaluation criteria

- Correctness, performance, clarity of the evaluate scenarios feature, after bug fixes
- Correctness, performance, clarity of the plan distribution feature implementation
- Test coverage: bugs are demonstrated by tests, new code is well tested, all tests pass
- Code quality improvements
