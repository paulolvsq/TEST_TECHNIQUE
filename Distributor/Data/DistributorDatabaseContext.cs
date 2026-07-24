using Distributor.Periods;
using Distributor.Plans;
using Distributor.Scenarios;
using Distributor.Stores;
using Distributor.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Distributor.Data;

public sealed class DistributorDatabaseContext : DbContext
{
    public DistributorDatabaseContext(DbContextOptions<DistributorDatabaseContext> options)
        : base(options) { }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<WarehouseCapacity> WarehouseCapacities => Set<WarehouseCapacity>();
    public DbSet<RouteCost> RouteCosts => Set<RouteCost>();
    public DbSet<StoreDemand> StoreDemands => Set<StoreDemand>();
    public DbSet<DistributionPlan> DistributionPlans => Set<DistributionPlan>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ScenarioWarehouseAdjustment> ScenarioWarehouseAdjustments => Set<ScenarioWarehouseAdjustment>();
    public DbSet<ScenarioStoreAdjustment> ScenarioStoreAdjustments => Set<ScenarioStoreAdjustment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ConfigureWarehouses(builder);
        ConfigureStores(builder);
        ConfigurePeriods(builder);
        ConfigureWarehouseCapacities(builder);
        ConfigureRouteCosts(builder);
        ConfigureStoreDemands(builder);
        ConfigureDistributionPlans(builder);
        ConfigureShipments(builder);
        ConfigureScenarios(builder);
        ConfigureScenarioWarehouseAdjustments(builder);
        ConfigureScenarioStoreAdjustments(builder);
    }

    private static void ConfigureWarehouses(ModelBuilder builder)
    {
        builder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(warehouse => warehouse.Id);
            entity.Property(warehouse => warehouse.Id).ValueGeneratedNever();
            entity.Property(warehouse => warehouse.Name).IsRequired();
        });
    }

    private static void ConfigureStores(ModelBuilder builder)
    {
        builder.Entity<Store>(entity =>
        {
            entity.ToTable("stores");
            entity.HasKey(store => store.Id);
            entity.Property(store => store.Id).ValueGeneratedNever();
            entity.Property(store => store.Name).IsRequired();
        });
    }

    private static void ConfigurePeriods(ModelBuilder builder)
    {
        builder.Entity<Period>(entity =>
        {
            entity.ToTable("periods");
            entity.HasKey(period => period.Id);
            entity.Property(period => period.Id).ValueGeneratedOnAdd();
            entity.Property(period => period.Year).IsRequired();
            entity.Property(period => period.Month).IsRequired();
            entity.HasIndex(period => new { period.Year, period.Month }).IsUnique();

            entity
                .HasMany(period => period.Capacities)
                .WithOne()
                .HasForeignKey(capacity => capacity.PeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(period => period.Demands)
                .WithOne()
                .HasForeignKey(demand => demand.PeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(period => period.Costs)
                .WithOne()
                .HasForeignKey(routeCost => routeCost.PeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(period => period.Capacities).HasField("_capacities");
            entity.Navigation(period => period.Demands).HasField("_demands");
            entity.Navigation(period => period.Costs).HasField("_costs");
        });
    }

    private static void ConfigureWarehouseCapacities(ModelBuilder builder)
    {
        builder.Entity<WarehouseCapacity>(entity =>
        {
            entity.ToTable("warehouse_capacities");
            entity.HasKey(capacity => new { capacity.PeriodId, capacity.WarehouseId });
            entity.Property(capacity => capacity.Units).IsRequired();

            entity
                .HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(capacity => capacity.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRouteCosts(ModelBuilder builder)
    {
        builder.Entity<RouteCost>(entity =>
        {
            entity.ToTable("route_costs");
            entity.HasKey(routeCost => new
            {
                routeCost.PeriodId,
                routeCost.WarehouseId,
                routeCost.StoreId,
            });
            entity.Property(routeCost => routeCost.UnitCost).IsRequired();

            entity
                .HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(routeCost => routeCost.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne<Store>()
                .WithMany()
                .HasForeignKey(routeCost => routeCost.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureStoreDemands(ModelBuilder builder)
    {
        builder.Entity<StoreDemand>(entity =>
        {
            entity.ToTable("store_demands");
            entity.HasKey(demand => new { demand.PeriodId, demand.StoreId });
            entity.Property(demand => demand.Units).IsRequired();

            entity.HasOne<Store>().WithMany().HasForeignKey(demand => demand.StoreId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDistributionPlans(ModelBuilder builder)
    {
        builder.Entity<DistributionPlan>(entity =>
        {
            entity.ToTable("distribution_plans");
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Id).ValueGeneratedOnAdd();
            entity.Property(plan => plan.Name).IsRequired();
            entity.Property(plan => plan.CreatedAt).IsRequired();
            entity.Property(plan => plan.StartPeriodId).IsRequired();
            entity.Property(plan => plan.EndPeriodId).IsRequired();

            entity
                .HasOne<Period>()
                .WithMany()
                .HasForeignKey(plan => plan.StartPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<Period>()
                .WithMany()
                .HasForeignKey(plan => plan.EndPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasMany(plan => plan.Shipments)
                .WithOne()
                .HasForeignKey(shipment => shipment.DistributionPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(plan => plan.Shipments).HasField("_shipments");
        });
    }

    private static void ConfigureShipments(ModelBuilder builder)
    {
        builder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(shipment => shipment.Id);
            entity.Property(shipment => shipment.Id).ValueGeneratedOnAdd();
            entity.Property(shipment => shipment.Units).IsRequired();

            entity
                .HasOne<Period>()
                .WithMany()
                .HasForeignKey(shipment => shipment.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(shipment => shipment.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<Store>()
                .WithMany()
                .HasForeignKey(shipment => shipment.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureScenarios(ModelBuilder builder)
    {
        builder.Entity<Scenario>(entity =>
        {
            entity.ToTable("scenarios");
            entity.HasKey(scenario => scenario.Id);
            entity.Property(scenario => scenario.Id).ValueGeneratedOnAdd();
            entity.Property(scenario => scenario.Name).IsRequired();

            entity
                .HasMany(scenario => scenario.WarehouseAdjustments)
                .WithOne()
                .HasForeignKey(adjustment => adjustment.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(scenario => scenario.StoreAdjustments)
                .WithOne()
                .HasForeignKey(adjustment => adjustment.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(scenario => scenario.WarehouseAdjustments).HasField("_warehouseAdjustments");
            entity.Navigation(scenario => scenario.StoreAdjustments).HasField("_storeAdjustments");
        });
    }

    private static void ConfigureScenarioWarehouseAdjustments(ModelBuilder builder)
    {
        builder.Entity<ScenarioWarehouseAdjustment>(entity =>
        {
            entity.ToTable("scenario_warehouse_adjustments");
            entity.HasKey(adjustment => new { adjustment.ScenarioId, adjustment.WarehouseId });
            entity.Property(adjustment => adjustment.Multiplier).IsRequired();

            entity
                .HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(adjustment => adjustment.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureScenarioStoreAdjustments(ModelBuilder builder)
    {
        builder.Entity<ScenarioStoreAdjustment>(entity =>
        {
            entity.ToTable("scenario_store_adjustments");
            entity.HasKey(adjustment => new { adjustment.ScenarioId, adjustment.StoreId });
            entity.Property(adjustment => adjustment.Multiplier).IsRequired();

            entity
                .HasOne<Store>()
                .WithMany()
                .HasForeignKey(adjustment => adjustment.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
