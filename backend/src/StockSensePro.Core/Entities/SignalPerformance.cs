namespace StockSensePro.Core.Entities
{
    public class SignalPerformance
    {
        public Guid Id { get; set; }
        public Guid TradingSignalId { get; set; }
        public TradingSignal TradingSignal { get; set; } = null!;

        public DateTime EvaluatedAt { get; set; }
        public decimal ActualReturn { get; set; }
        public decimal BenchmarkReturn { get; set; }
        public bool WasProfitable { get; set; }
        public int DaysHeld { get; set; }
        public decimal? EntryPrice { get; set; }
        public decimal? ExitPrice { get; set; }
        public decimal? MaxDrawdown { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
