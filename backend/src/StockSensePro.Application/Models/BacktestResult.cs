namespace StockSensePro.Application.Models
{
    public class BacktestResult
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal AverageReturn { get; set; }
        public decimal CumulativeReturn { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal WinRate => TotalTrades == 0 ? 0 : Math.Round((decimal)WinningTrades / TotalTrades * 100, 2);
        public List<BacktestTradeResult> Trades { get; set; } = new();
    }

    public class BacktestTradeResult
    {
        public Guid SignalId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime ExitDate { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal Return { get; set; }
        public bool WasProfitable { get; set; }
        public string SignalType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
