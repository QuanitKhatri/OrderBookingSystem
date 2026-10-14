using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.Transactions;
using Distribution.Services.Sequencing;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Expenses;

public class ExpenseCategoryService(DistributionDbContext db) : IExpenseCategoryService
{
    public async Task<List<(int Id, string Name)>> GetActiveLookupAsync(CancellationToken ct = default)
    {
        return await db.ExpenseCategories.Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new ValueTuple<int, string>(c.Id, c.Name))
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(string name, ExpenseType type, string performedBy, CancellationToken ct = default)
    {
        var entity = new ExpenseCategory
        {
            Name = name.Trim(),
            Type = type,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.ExpenseCategories.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public class ExpenseService(DistributionDbContext db, ISequenceService sequenceService) : IExpenseService
{
    public async Task RecordAsync(ExpenseInputModel model, string performedBy, CancellationToken ct = default)
    {
        var voucherNo = await sequenceService.NextNumberAsync("VoucherNo", "EXP-", ct);
        db.Expenses.Add(new Expense
        {
            VoucherNo = voucherNo,
            Date = model.Date,
            ExpenseCategoryId = model.CategoryId,
            Amount = model.Amount,
            PaidFrom = model.PaidFrom,
            Description = model.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<ExpenseListItem>> GetTodaysExpensesAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await SearchAsync(today, today, null, ct);
    }

    public async Task<List<ExpenseListItem>> SearchAsync(DateTime? from, DateTime? to, int? categoryId, CancellationToken ct = default)
    {
        var q = db.Expenses.Include(e => e.ExpenseCategory).AsQueryable();
        if (from.HasValue) q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue) q = q.Where(e => e.Date <= to.Value);
        if (categoryId.HasValue) q = q.Where(e => e.ExpenseCategoryId == categoryId.Value);

        return await q.OrderByDescending(e => e.Id)
            .Select(e => new ExpenseListItem(e.Id, e.VoucherNo, e.Date, e.ExpenseCategory.Name, e.Amount, e.PaidFrom.ToString(), e.Description))
            .ToListAsync(ct);
    }

    public async Task<List<ExpenseCategorySummary>> GetCategorySummaryAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var expenses = await db.Expenses.AsNoTracking().Include(e => e.ExpenseCategory)
            .Where(e => e.Date >= monthStart)
            .ToListAsync(ct);

        var mtdTotal = expenses.Sum(e => e.Amount);

        return expenses.GroupBy(e => e.ExpenseCategory.Name)
            .Select(g =>
            {
                var mtd = g.Sum(e => e.Amount);
                var todayTotal = g.Where(e => e.Date.Date == today).Sum(e => e.Amount);
                var share = mtdTotal > 0 ? mtd / mtdTotal * 100 : 0;
                return new ExpenseCategorySummary(g.Key, todayTotal, mtd, share);
            })
            .OrderByDescending(s => s.MonthToDateTotal)
            .ToList();
    }
}
