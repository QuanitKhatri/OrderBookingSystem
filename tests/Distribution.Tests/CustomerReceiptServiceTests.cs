using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Masters;
using Distribution.Services.Sales;
using Distribution.Services.Sequencing;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Tests;

public class CustomerReceiptServiceTests : IAsyncLifetime
{
    private DistributionDbContext _db = null!;
    private CustomerReceiptService _service = null!;
    private CustomerBalanceService _balances = null!;

    private int _areaId, _customerId;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("DISTRIBUTION_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=distribution_dev;Username=distribution_app;Password=distribution_dev_pwd";
        var options = new DbContextOptionsBuilder<DistributionDbContext>().UseNpgsql(connectionString).Options;
        _db = new DistributionDbContext(options);

        _balances = new CustomerBalanceService(_db);
        _service = new CustomerReceiptService(_db, new SequenceService(_db), _balances);

        var area = new Area { Name = $"TestArea-{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow, CreatedBy = "test" };
        _db.Areas.Add(area);
        await _db.SaveChangesAsync();
        _areaId = area.Id;

        var customer = new Customer
        {
            Code = $"RC-{Guid.NewGuid():N}"[..10],
            ShopName = "Receipt Test Shop",
            AreaId = _areaId,
            OpeningBalance = 5000,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        _customerId = customer.Id;
    }

    public async Task DisposeAsync()
    {
        _db.CustomerReceipts.RemoveRange(await _db.CustomerReceipts.Where(r => r.CustomerId == _customerId).ToListAsync());
        var customer = await _db.Customers.FindAsync(_customerId);
        if (customer is not null) _db.Customers.Remove(customer);
        var area = await _db.Areas.FindAsync(_areaId);
        if (area is not null) _db.Areas.Remove(area);
        await _db.SaveChangesAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ReducesCustomerBalance()
    {
        var result = await _service.CreateAsync(
            new ReceiptInputModel(_customerId, DateTime.Today, 2000, PaidBy.Cash, "Part payment"), "test");

        Assert.StartsWith("RCPT-", result.ReceiptNo);
        Assert.Equal(5000, result.PreviousBalance);
        Assert.Equal(3000, result.NewBalance);
        Assert.Equal(3000, await _balances.GetBalanceAsync(_customerId));
    }

    [Fact]
    public async Task CreateAsync_AppearsInLedgerAsCredit()
    {
        await _service.CreateAsync(new ReceiptInputModel(_customerId, DateTime.Today, 1500, PaidBy.Bank, null), "test");

        var ledger = new CustomerLedgerService(_db, _balances);
        var entries = await ledger.GetLedgerAsync(_customerId, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

        var receipt = Assert.Single(entries, e => e.Type == "Receipt");
        Assert.Equal(1500, receipt.Credit);
        Assert.Equal(0, receipt.Debit);
        Assert.Equal(3500, entries[^1].RunningBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateAsync_RejectsNonPositiveAmount(decimal amount)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(new ReceiptInputModel(_customerId, DateTime.Today, amount, PaidBy.Cash, null), "test"));

        Assert.Equal("Amount must be greater than zero.", ex.Message);
        Assert.Equal(5000, await _balances.GetBalanceAsync(_customerId));
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownCustomer()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(new ReceiptInputModel(-1, DateTime.Today, 100, PaidBy.Cash, null), "test"));
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsStandaloneReceiptWithoutBillNo()
    {
        var created = await _service.CreateAsync(
            new ReceiptInputModel(_customerId, DateTime.Today, 750, PaidBy.Cash, "Recovery"), "test");

        var row = Assert.Single(await _service.GetRecentAsync(), r => r.ReceiptNo == created.ReceiptNo);
        Assert.Null(row.BillNo);
        Assert.Equal(750, row.Amount);
        Assert.Equal("Cash", row.PaidBy);
        Assert.Equal("Recovery", row.Remarks);
    }
}
