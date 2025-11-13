namespace StockSensePro.Application.Models
{
    public class BacktestSummary
    {
        public string Symbol { get; set; } = string.Empty;
        public int TotalSignals { get; set; }
        public int EvaluatedSignals { get; set; }
        public decimal AverageReturn { get; set; }
        public decimal CumulativeReturn { get; set; }
        public decimal WinRate { get; set; }
        public decimal MaxDrawdown { get; set; }
        public DateTime? LastEvaluatedAt { get; set; }
    }
}
