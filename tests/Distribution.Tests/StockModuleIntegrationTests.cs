using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Masters;
using Distribution.Services.Sequencing;
using Distribution.Services.Stock;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Tests;

/// <summary>
/// Exercises the Stock module services against the real dev database (no UI involved).
/// Each test creates its own throwaway product so runs don't interfere with each other.
/// </summary>
public class StockModuleIntegrationTests : IAsyncLifetime
{
    private DistributionDbContext _db = null!;
    private IStockQueryService _stockQuery = null!;
    private ISequenceService _sequenceService = null!;
    private IOpeningStockService _openingStockService = null!;
    private IPurchaseService _purchaseService = null!;
    private IDamageService _damageService = null!;
    private IDamageReasonService _damageReasonService = null!;

    private readonly List<int> _productIdsToCleanUp = [];
    private int _companyId;
    private int _categoryId;
    private int _supplierId;
    private int _reasonId;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("DISTRIBUTION_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=distribution_dev;Username=distribution_app;Password=distribution_dev_pwd";

        var options = new DbContextOptionsBuilder<DistributionDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        _db = new DistributionDbContext(options);

        _stockQuery = new StockQueryService(_db);
        _sequenceService = new SequenceService(_db);
        _openingStockService = new OpeningStockService(_db, _stockQuery);
        _purchaseService = new PurchaseService(_db, _stockQuery);
        _damageReasonService = new DamageReasonService(_db);
        _damageService = new DamageService(_db, _stockQuery, _sequenceService);

        _companyId = await _db.Companies.Select(c => c.Id).FirstAsync();
        _categoryId = await _db.Categories.Select(c => c.Id).FirstOrDefaultAsync();
        if (_categoryId == 0)
        {
            var cat = new Category { Name = $"TestCat-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow, CreatedBy = "test" };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            _categoryId = cat.Id;
        }

        var supplier = new Supplier
        {
            Code = $"TSUP-{Guid.NewGuid():N}"[..10],
            Name = "Integration Test Supplier",
            CompanyId = _companyId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _db.Suppliers.Add(supplier);

        var reason = new Domain.Transactions.DamageReason { Name = $"TestReason-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow, CreatedBy = "test" };
        _db.DamageReasons.Add(reason);

        await _db.SaveChangesAsync();
        _supplierId = supplier.Id;
        _reasonId = reason.Id;
    }

    public async Task DisposeAsync()
    {
        // Clean up everything created by these tests so the dev DB stays tidy.
        var movements = await _db.StockMovements.Where(m => _productIdsToCleanUp.Contains(m.ProductId)).ToListAsync();
        _db.StockMovements.RemoveRange(movements);

        var damageEntries = await _db.DamageEntries.Where(d => _productIdsToCleanUp.Contains(d.ProductId)).ToListAsync();
        _db.DamageEntries.RemoveRange(damageEntries);

        var purchaseItems = await _db.PurchaseItems.Where(pi => _productIdsToCleanUp.Contains(pi.ProductId)).ToListAsync();
        var purchaseIds = purchaseItems.Select(pi => pi.PurchaseId).Distinct().ToList();
        _db.PurchaseItems.RemoveRange(purchaseItems);

        var expenses = await _db.Expenses.Where(e => e.PurchaseId != null && purchaseIds.Contains(e.PurchaseId.Value)).ToListAsync();
        _db.Expenses.RemoveRange(expenses);

        var purchases = await _db.Purchases.Where(p => purchaseIds.Contains(p.Id)).ToListAsync();
        _db.Purchases.RemoveRange(purchases);

        var products = await _db.Products.Where(p => _productIdsToCleanUp.Contains(p.Id)).ToListAsync();
        _db.Products.RemoveRange(products);

        var reason = await _db.DamageReasons.FindAsync(_reasonId);
        if (reason is not null) _db.DamageReasons.Remove(reason);

        var supplier = await _db.Suppliers.FindAsync(_supplierId);
        if (supplier is not null) _db.Suppliers.Remove(supplier);

        await _db.SaveChangesAsync();
        await _db.DisposeAsync();
    }

    private async Task<int> CreateTestProductAsync(decimal initialAvgCost = 0)
    {
        var product = new Product
        {
            Code = $"TST-{Guid.NewGuid():N}"[..12],
            Name = "Integration Test Product",
            CompanyId = _companyId,
            CategoryId = _categoryId,
            Unit = UnitType.Pcs,
            PcsPerUnit = 1,
            PurchaseRate = 0,
            SaleRate = 0,
            AvgCost = initialAvgCost,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        _productIdsToCleanUp.Add(product.Id);
        return product.Id;
    }

    [Fact]
    public async Task OpeningStock_RecordsMovementAndSetsAvgCost()
    {
        var productId = await CreateTestProductAsync();

        await _openingStockService.RecordAsync(
            new OpeningStockEntryModel(productId, DateTime.UtcNow.Date, 50, 20m), "test");

        var warehouseId = await _stockQuery.GetDefaultWarehouseIdAsync();
        var qty = await _stockQuery.GetCurrentStockAsync(productId, warehouseId);
        var product = await _db.Products.FindAsync(productId);

        Assert.Equal(50, qty);
        Assert.Equal(20m, product!.AvgCost);
    }

    [Fact]
    public async Task Purchase_BlendsAvgCost_AndWritesStockMovement()
    {
        var productId = await CreateTestProductAsync();
        await _openingStockService.RecordAsync(
            new OpeningStockEntryModel(productId, DateTime.UtcNow.Date, 100, 85m), "test");

        var result = await _purchaseService.CreateAsync(
            new PurchaseInputModel(
                $"INV-{Guid.NewGuid():N}"[..10], _supplierId, DateTime.UtcNow.Date, null, 0, null,
                [new PurchaseItemInput(productId, 50, 100m)]),
            "test");

        var warehouseId = await _stockQuery.GetDefaultWarehouseIdAsync();
        var qty = await _stockQuery.GetCurrentStockAsync(productId, warehouseId);
        var product = await _db.Products.FindAsync(productId);

        // (100*85 + 50*100) / 150 = 90
        Assert.Equal(90m, product!.AvgCost);
        Assert.Equal(150, qty);
        Assert.Equal(85m, result.Items[0].AvgCostBefore);
        Assert.Equal(90m, result.Items[0].AvgCostAfter);
        Assert.Equal(5000m, result.TotalAmount); // 50 * 100
    }

    [Fact]
    public async Task Purchase_WithBiltiCharges_CreatesLinkedExpense()
    {
        var productId = await CreateTestProductAsync();

        var result = await _purchaseService.CreateAsync(
            new PurchaseInputModel(
                $"INV-{Guid.NewGuid():N}"[..10], _supplierId, DateTime.UtcNow.Date, "TRK-99", 500m, null,
                [new PurchaseItemInput(productId, 10, 50m)]),
            "test");

        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.PurchaseId == result.PurchaseId);

        Assert.NotNull(expense);
        Assert.Equal(500m, expense!.Amount);
    }

    [Fact]
    public async Task Damage_ReducesStock_AndRecordsLossAtCurrentAvgCost()
    {
        var productId = await CreateTestProductAsync();
        await _openingStockService.RecordAsync(
            new OpeningStockEntryModel(productId, DateTime.UtcNow.Date, 20, 40m), "test");

        await _damageService.RecordAsync(
            new DamageEntryModel(productId, DateTime.UtcNow.Date, 5, _reasonId, FoundAt.Godown, "test damage"),
            "test");

        var warehouseId = await _stockQuery.GetDefaultWarehouseIdAsync();
        var qty = await _stockQuery.GetCurrentStockAsync(productId, warehouseId);

        Assert.Equal(15, qty); // 20 - 5
    }

    [Fact]
    public async Task Damage_ExceedingAvailableStock_IsBlocked()
    {
        var productId = await CreateTestProductAsync();
        await _openingStockService.RecordAsync(
            new OpeningStockEntryModel(productId, DateTime.UtcNow.Date, 5, 40m), "test");

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _damageService.RecordAsync(
                new DamageEntryModel(productId, DateTime.UtcNow.Date, 10, _reasonId, FoundAt.Godown, null),
                "test"));
    }

    [Fact]
    public async Task SequenceService_GeneratesUniqueIncrementingNumbers()
    {
        var sequenceName = $"TestSeq-{Guid.NewGuid():N}";

        var first = await _sequenceService.NextNumberAsync(sequenceName, "TST-");
        var second = await _sequenceService.NextNumberAsync(sequenceName, "TST-");

        Assert.Equal("TST-00001", first);
        Assert.Equal("TST-00002", second);

        // Clean up the row this test created.
        var row = await _db.Sequences.FindAsync(sequenceName);
        if (row is not null)
        {
            _db.Sequences.Remove(row);
            await _db.SaveChangesAsync();
        }
    }
}
