namespace Commerce.Application.Forecasting;

/// <summary>
/// Decision 7: forecasting is arithmetic, not LLM. Weighted moving average with
/// a variance-based confidence — deliberately simple, explainable, unit-tested.
/// </summary>
public static class ForecastMath
{
    /// <summary>
    /// Daily demand rate from a daily-sales series. Recent 14 days weigh double
    /// against the trailing window so trends show up without whiplash.
    /// </summary>
    public static double DailyRate(IReadOnlyList<int> dailyUnits)
    {
        if (dailyUnits.Count == 0) return 0;

        var recent = dailyUnits.TakeLast(14).ToArray();
        double recentAvg = recent.Length > 0 ? recent.Average() : 0;
        double overallAvg = dailyUnits.Average();
        return Math.Max(0, (2 * recentAvg + overallAvg) / 3);
    }

    public static int PredictUnits(double dailyRate, int horizonDays)
        => (int)Math.Round(dailyRate * horizonDays, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Confidence from the coefficient of variation of daily sales: steady
    /// sellers ≈ 0.9, erratic ones drift toward 0.2. Zero history = 0.
    /// </summary>
    public static double Confidence(IReadOnlyList<int> dailyUnits)
    {
        if (dailyUnits.Count < 7) return 0;
        double mean = dailyUnits.Average();
        if (mean <= 0) return 0;

        double variance = dailyUnits.Sum(u => (u - mean) * (u - mean)) / dailyUnits.Count;
        double cv = Math.Sqrt(variance) / mean;
        return Math.Clamp(1.0 - cv / 3.0, 0.2, 0.95);
    }

    /// <summary>Days until stock crosses the safety floor at the current rate (null = no meaningful demand).</summary>
    public static int? DaysUntilStockout(int currentStock, int safetyStock, double dailyRate)
        => dailyRate < 0.01 ? null : Math.Max(0, (int)Math.Floor((currentStock - safetyStock) / dailyRate));

    /// <summary>
    /// Reorder quantity: cover lead time plus a 30-day cycle at the forecast
    /// rate, restore safety stock, minus what's still on the shelf.
    /// </summary>
    public static int ReorderQuantity(int currentStock, int safetyStock, int leadTimeDays, double dailyRate)
    {
        int needed = (int)Math.Ceiling(dailyRate * (leadTimeDays + 30)) + safetyStock - currentStock;
        return Math.Max(0, needed);
    }
}
