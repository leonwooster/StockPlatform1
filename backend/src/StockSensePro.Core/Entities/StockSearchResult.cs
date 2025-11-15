namespace StockSensePro.Core.Entities
{
    public class StockSearchResult
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public decimal? MatchScore { get; set; }
    }
}
