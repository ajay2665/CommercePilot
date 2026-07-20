using StorePilot.Application.Forecasting;

namespace StorePilot.UnitTests;

public class ForecastMathTests
{
    [Fact]
    public void SteadySellerRateMatchesAverage()
    {
        int[] daily = Enumerable.Repeat(4, 90).ToArray();
        Assert.Equal(4.0, ForecastMath.DailyRate(daily), 1);
        Assert.Equal(120, ForecastMath.PredictUnits(4.0, 30));
    }

    [Fact]
    public void RecentTrendWeighsDouble()
    {
        // 76 quiet days then a 14-day spike: rate must sit well above the flat average.
        int[] daily = [.. Enumerable.Repeat(1, 76), .. Enumerable.Repeat(10, 14)];
        double rate = ForecastMath.DailyRate(daily);
        Assert.True(rate > daily.Average(), $"rate {rate} should exceed overall average {daily.Average()}");
    }

    [Fact]
    public void NoSalesMeansZeroRateAndZeroConfidence()
    {
        int[] daily = new int[90];
        Assert.Equal(0, ForecastMath.DailyRate(daily));
        Assert.Equal(0, ForecastMath.Confidence(daily));
        Assert.Null(ForecastMath.DaysUntilStockout(50, 10, 0));
    }

    [Fact]
    public void SteadySalesGiveHighConfidence()
    {
        int[] steady = Enumerable.Repeat(5, 60).ToArray();
        Assert.True(ForecastMath.Confidence(steady) >= 0.9);
    }

    [Fact]
    public void StockoutDaysAndReorderQuantity()
    {
        // 100 on hand, 10 safety, 3/day → (100-10)/3 = 30 days runway.
        Assert.Equal(30, ForecastMath.DaysUntilStockout(100, 10, 3.0));
        // Cover (14 lead + 30 cycle) * 3/day + 10 safety - 20 on hand = 122.
        Assert.Equal(122, ForecastMath.ReorderQuantity(20, 10, 14, 3.0));
        Assert.Equal(0, ForecastMath.ReorderQuantity(500, 10, 14, 1.0));
    }
}
