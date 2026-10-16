using Distribution.Data;
using Distribution.Domain.Transactions;
using Distribution.Services.Sequencing;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Sales;

public class CustomerReceiptService(DistributionDbContext db, ISequenceService sequenceService, ICustomerBalanceService balanceService) : ICustomerReceiptService
{
    private const int RemarksMaxLength = 500;

    public async Task<ReceiptResult> CreateAsync(ReceiptInputModel model, string performedBy, CancellationToken ct = default)
    {
        if (model.Amount <= 0)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }

        var remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim();
        if (remarks?.Length > RemarksMaxLength)
        {
            throw new InvalidOperationException($"Remarks cannot be longer than {RemarksMaxLength} characters.");
        }

        if (!await db.Customers.AnyAsync(c => c.Id == model.CustomerId && !c.IsDeleted, ct))
        {
            throw new InvalidOperationException("Customer not found.");
        }

        var previousBalance = await balanceService.GetBalanceAsync(model.CustomerId, ct);

        var receipt = new CustomerReceipt
        {
            // Same sequence as cash taken on bills, so every receipt shares one RCPT- numbering.
            ReceiptNo = await sequenceService.NextNumberAsync("ReceiptNo", "RCPT-", ct),
            CustomerId = model.CustomerId,
            Date = model.Date.Date,
            Amount = model.Amount,
            PaidBy = model.PaidBy,
            Remarks = remarks,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };

        db.CustomerReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);

        return new ReceiptResult(receipt.Id, receipt.ReceiptNo, previousBalance, previousBalance - model.Amount);
    }

    public async Task<List<ReceiptListItem>> GetRecentAsync(int take = 50, CancellationToken ct = default)
    {
        return await db.CustomerReceipts.AsNoTracking()
            .OrderByDescending(r => r.Id)
            .Take(take)
            .Select(r => new ReceiptListItem(
                r.Id, r.ReceiptNo, r.Date, r.Customer.Code, r.Customer.ShopName, r.Amount,
                r.PaidBy.ToString(), r.Sale != null ? r.Sale.BillNo : null, r.Remarks))
            .ToListAsync(ct);
    }
}
