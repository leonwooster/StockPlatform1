namespace StockSensePro.Core.Entities
{
    public class FundamentalData
    {
        public string Symbol { get; set; } = string.Empty;
        
        // Valuation Ratios
        public decimal? PERatio { get; set; }
        public decimal? PEGRatio { get; set; }
        public decimal? PriceToBook { get; set; }
        public decimal? PriceToSales { get; set; }
        public decimal? EnterpriseValue { get; set; }
        public decimal? EVToEBITDA { get; set; }
        
        // Profitability Metrics
        public decimal? ProfitMargin { get; set; }
        public decimal? OperatingMargin { get; set; }
        public decimal? ReturnOnEquity { get; set; }
        public decimal? ReturnOnAssets { get; set; }
        
        // Growth Metrics
        public decimal? RevenueGrowth { get; set; }
        public decimal? EarningsGrowth { get; set; }
        public decimal? EPS { get; set; }
        
        // Dividend Information
        public decimal? DividendYield { get; set; }
        public decimal? PayoutRatio { get; set; }
        
        // Financial Health
        public decimal? CurrentRatio { get; set; }
        public decimal? DebtToEquity { get; set; }
        public decimal? QuickRatio { get; set; }
        
        public DateTime LastUpdated { get; set; }
    }
}
