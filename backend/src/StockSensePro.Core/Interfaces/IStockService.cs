using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;

namespace StockSensePro.Core.Interfaces
{
    public interface IStockService
    {
        // Repository methods
        Task<Stock?> GetStockBySymbolAsync(string symbol);
        Task<IEnumerable<Stock>> GetAllStocksAsync();
        Task<Stock> CreateStockAsync(Stock stock);
        Task<Stock> UpdateStockAsync(Stock stock);
        Task<bool> DeleteStockAsync(string symbol);

        // Cached data provider methods
        Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
        Task<List<StockPrice>> GetHistoricalPricesAsync(
            string symbol,
            DateTime startDate,
            DateTime endDate,
            TimeInterval interval = TimeInterval.Daily,
            CancellationToken cancellationToken = default);
        Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default);
        Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default);
        Task<List<StockSearchResult>> SearchSymbolsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
        
        // Cache management
        Task WarmCacheAsync(List<string> symbols, CancellationToken cancellationToken = default);
    }
}
