namespace StockSensePro.Application.Models
{
    public class BacktestRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int HoldingPeriodDays { get; set; } = 5;
        public decimal? StopLossPercent { get; set; }
        public decimal? TakeProfitPercent { get; set; }
        public string Strategy { get; set; } = "default";
    }
}
