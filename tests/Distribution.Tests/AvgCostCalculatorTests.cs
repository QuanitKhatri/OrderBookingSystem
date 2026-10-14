using Distribution.Services.Stock;

namespace Distribution.Tests;

public class AvgCostCalculatorTests
{
    [Fact]
    public void FirstPurchase_WhenNoExistingStock_UsesPurchaseRateAsIs()
    {
        var result = AvgCostCalculator.NewAverage(currentQty: 0, currentAvgCost: 0, receivedQty: 100, purchaseRate: 85m);

        Assert.Equal(85m, result);
    }

    [Fact]
    public void SecondPurchase_AtHigherRate_BlendsIntoWeightedAverage()
    {
        // 100 units @ 85 already in stock, receiving 50 more @ 100
        var result = AvgCostCalculator.NewAverage(currentQty: 100, currentAvgCost: 85m, receivedQty: 50, purchaseRate: 100m);

        // (100*85 + 50*100) / 150 = 90
        Assert.Equal(90m, result);
    }

    [Fact]
    public void SecondPurchase_AtLowerRate_BlendsDownward()
    {
        var result = AvgCostCalculator.NewAverage(currentQty: 100, currentAvgCost: 90m, receivedQty: 100, purchaseRate: 70m);

        // (100*90 + 100*70) / 200 = 80
        Assert.Equal(80m, result);
    }

    [Fact]
    public void ThirdPurchase_AfterTwoPriorPurchases_ContinuesCompounding()
    {
        var afterFirst = AvgCostCalculator.NewAverage(0, 0, 100, 85m);
        var afterSecond = AvgCostCalculator.NewAverage(100, afterFirst, 50, 100m);
        var afterThird = AvgCostCalculator.NewAverage(150, afterSecond, 150, 80m);

        // afterFirst = 85, afterSecond = 90, afterThird = (150*90 + 150*80)/300 = 85
        Assert.Equal(85m, afterThird);
    }

    [Fact]
    public void NegativeCurrentQty_TreatedSameAsZero_UsesPurchaseRate()
    {
        // Defensive: stock ledger should never go negative, but the formula must not blow up if it does.
        var result = AvgCostCalculator.NewAverage(currentQty: -5, currentAvgCost: 999m, receivedQty: 10, purchaseRate: 50m);

        Assert.Equal(50m, result);
    }
}
