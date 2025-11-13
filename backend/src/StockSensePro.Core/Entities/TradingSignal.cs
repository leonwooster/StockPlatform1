namespace StockSensePro.Core.Entities
{
    public class TradingSignal
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string Strategy { get; set; } = string.Empty;
        public string SignalType { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public decimal? ActualReturn { get; set; }
        public int? HoldingPeriodDays { get; set; }

        public ICollection<SignalPerformance> Performances { get; set; } = new List<SignalPerformance>();
    }
}
