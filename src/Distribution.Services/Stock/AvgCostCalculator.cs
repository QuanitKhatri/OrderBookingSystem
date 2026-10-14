namespace Distribution.Services.Stock;

public static class AvgCostCalculator
{
    /// <summary>
    /// Weighted-average cost after receiving <paramref name="receivedQty"/> units at <paramref name="purchaseRate"/>,
    /// given the product currently holds <paramref name="currentQty"/> units valued at <paramref name="currentAvgCost"/>.
    /// </summary>
    public static decimal NewAverage(decimal currentQty, decimal currentAvgCost, decimal receivedQty, decimal purchaseRate)
    {
        if (currentQty <= 0)
        {
            return purchaseRate;
        }

        return (currentQty * currentAvgCost + receivedQty * purchaseRate) / (currentQty + receivedQty);
    }
}
