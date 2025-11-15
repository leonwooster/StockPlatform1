using StockSensePro.Core.Enums;

namespace StockSensePro.Core.Entities
{
    public class MarketData
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public long Volume { get; set; }
        public decimal? BidPrice { get; set; }
        public decimal? AskPrice { get; set; }
        public decimal DayHigh { get; set; }
        public decimal DayLow { get; set; }
        public decimal? FiftyTwoWeekHigh { get; set; }
        public decimal? FiftyTwoWeekLow { get; set; }
        public long? AverageVolume { get; set; }
        public long? MarketCap { get; set; }
        public DateTime Timestamp { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public MarketState MarketState { get; set; }
    }
}
