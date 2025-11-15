using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;

namespace StockSensePro.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for stock data providers, enabling the Adapter pattern
    /// to support multiple data sources (Yahoo Finance, Polygon.io, Alpha Vantage, etc.)
    /// </summary>
    public interface IStockDataProvider
    {
        /// <summary>
        /// Fetches current market data for a single stock symbol
        /// </summary>
        /// <param name="symbol">The stock symbol (e.g., "AAPL")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Market data including current price, volume, and other metrics</returns>
        Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches current market data for multiple stock symbols in a single request
        /// </summary>
        /// <param name="symbols">List of stock symbols</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of market data for each symbol</returns>
        Task<List<MarketData>> GetQuotesAsync(List<string> symbols, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches historical price data (OHLCV) for a stock symbol
        /// </summary>
        /// <param name="symbol">The stock symbol</param>
        /// <param name="startDate">Start date for historical data</param>
        /// <param name="endDate">End date for historical data</param>
        /// <param name="interval">Time interval (Daily, Weekly, Monthly)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of historical prices</returns>
        Task<List<StockPrice>> GetHistoricalPricesAsync(
            string symbol,
            DateTime startDate,
            DateTime endDate,
            TimeInterval interval = TimeInterval.Daily,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches fundamental data including financial ratios and metrics
        /// </summary>
        /// <param name="symbol">The stock symbol</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Fundamental data including P/E ratio, profit margins, etc.</returns>
        Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches company profile information
        /// </summary>
        /// <param name="symbol">The stock symbol</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Company profile including name, sector, industry, description, etc.</returns>
        Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for stocks by company name or symbol
        /// </summary>
        /// <param name="query">Search query (company name or symbol)</param>
        /// <param name="limit">Maximum number of results to return (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching stock search results</returns>
        Task<List<StockSearchResult>> SearchSymbolsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the data provider API is healthy and accessible
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the API is healthy, false otherwise</returns>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    }
}
