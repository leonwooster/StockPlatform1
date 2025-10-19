namespace StockSensePro.Core.Entities
{
    public class Stock
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal PreviousClose { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public long Volume { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
