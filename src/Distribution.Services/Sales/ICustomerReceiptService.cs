namespace Distribution.Services.Sales;

public interface ICustomerReceiptService
{
    /// <summary>Records a payment from a customer against their outstanding balance (not tied to a bill).</summary>
    Task<ReceiptResult> CreateAsync(ReceiptInputModel model, string performedBy, CancellationToken ct = default);

    /// <summary>Most recent receipts, newest first — both stand-alone payments and cash taken on bills.</summary>
    Task<List<ReceiptListItem>> GetRecentAsync(int take = 50, CancellationToken ct = default);
}
