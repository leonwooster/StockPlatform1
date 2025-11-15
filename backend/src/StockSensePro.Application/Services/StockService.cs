using Microsoft.Extensions.Logging;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IStockDataProvider _stockDataProvider;
        private readonly ICacheService _cacheService;
        private readonly ILogger<StockService> _logger;

        // Cache TTL constants (in seconds)
        private const int QuoteTTL = 900;           // 15 minutes
        private const int HistoricalTTL = 86400;    // 24 hours
        private const int FundamentalsTTL = 21600;  // 6 hours
        private const int ProfileTTL = 604800;      // 7 days
        private const int SearchTTL = 3600;         // 1 hour

        public StockService(
            IStockRepository stockRepository,
            IStockDataProvider stockDataProvider,
            ICacheService cacheService,
            ILogger<StockService> logger)
        {
            _stockRepository = stockRepository;
            _stockDataProvider = stockDataProvider;
            _cacheService = cacheService;
            _logger = logger;
        }

        // Existing repository methods
        public async Task<Stock?> GetStockBySymbolAsync(string symbol)
        {
            return await _stockRepository.GetBySymbolAsync(symbol);
        }

        public async Task<IEnumerable<Stock>> GetAllStocksAsync()
        {
            return await _stockRepository.GetAllAsync();
        }

        public async Task<Stock> CreateStockAsync(Stock stock)
        {
            await _stockRepository.AddAsync(stock);
            return stock;
        }

        public async Task<Stock> UpdateStockAsync(Stock stock)
        {
            await _stockRepository.UpdateAsync(stock);
            return stock;
        }

        public async Task<bool> DeleteStockAsync(string symbol)
        {
            await _stockRepository.DeleteAsync(symbol);
            return true;
        }

        // New cached methods implementing cache-aside pattern

        /// <summary>
        /// Gets current market data for a symbol with caching
        /// Cache key: quote:{symbol}
        /// TTL: 15 minutes
        /// Fallback: Returns stale cached data if API fails
        /// </summary>
        public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"quote:{symbol}";
            var staleCacheKey = $"stale:{cacheKey}";

            try
            {
                // Check cache first
                var cachedData = await _cacheService.GetAsync<MarketData>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache HIT for quote {Symbol}", symbol);
                    return cachedData;
                }

                _logger.LogInformation("Cache MISS for quote {Symbol}. Fetching from API", symbol);

                // Fetch from API
                var marketData = await _stockDataProvider.GetQuoteAsync(symbol, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, marketData, TimeSpan.FromSeconds(QuoteTTL));
                
                // Store in stale cache with longer TTL for fallback (24 hours)
                await _cacheService.SetAsync(staleCacheKey, marketData, TimeSpan.FromHours(24));

                _logger.LogInformation("Cached quote for {Symbol} with TTL {TTL}s", symbol, QuoteTTL);

                return marketData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quote for {Symbol}. Attempting fallback to stale cache", symbol);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<MarketData>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning("Serving STALE cached data for quote {Symbol} due to API failure", symbol);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError("No cached data available for {Symbol}. Unable to serve request", symbol);
                throw;
            }
        }

        /// <summary>
        /// Gets historical prices for a symbol with caching
        /// Cache key: historical:{symbol}:{start}:{end}:{interval}
        /// TTL: 24 hours
        /// Fallback: Returns stale cached data if API fails
        /// </summary>
        public async Task<List<StockPrice>> GetHistoricalPricesAsync(
            string symbol,
            DateTime startDate,
            DateTime endDate,
            TimeInterval interval = TimeInterval.Daily,
            CancellationToken cancellationToken = default)
        {
            var startStr = startDate.ToString("yyyy-MM-dd");
            var endStr = endDate.ToString("yyyy-MM-dd");
            var cacheKey = $"historical:{symbol}:{startStr}:{endStr}:{interval}";
            var staleCacheKey = $"stale:{cacheKey}";

            try
            {
                // Check cache first
                var cachedData = await _cacheService.GetAsync<List<StockPrice>>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache HIT for historical prices {Symbol} ({Start} to {End})", symbol, startStr, endStr);
                    return cachedData;
                }

                _logger.LogInformation("Cache MISS for historical prices {Symbol}. Fetching from API", symbol);

                // Fetch from API
                var historicalPrices = await _stockDataProvider.GetHistoricalPricesAsync(
                    symbol, startDate, endDate, interval, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, historicalPrices, TimeSpan.FromSeconds(HistoricalTTL));
                
                // Store in stale cache with longer TTL for fallback (7 days)
                await _cacheService.SetAsync(staleCacheKey, historicalPrices, TimeSpan.FromDays(7));

                _logger.LogInformation("Cached historical prices for {Symbol} with TTL {TTL}s", symbol, HistoricalTTL);

                return historicalPrices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical prices for {Symbol}. Attempting fallback to stale cache", symbol);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<List<StockPrice>>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning("Serving STALE cached data for historical prices {Symbol} due to API failure", symbol);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError("No cached data available for historical prices {Symbol}. Unable to serve request", symbol);
                throw;
            }
        }

        /// <summary>
        /// Gets fundamental data for a symbol with caching
        /// Cache key: fundamentals:{symbol}
        /// TTL: 6 hours
        /// Fallback: Returns stale cached data if API fails
        /// </summary>
        public async Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"fundamentals:{symbol}";
            var staleCacheKey = $"stale:{cacheKey}";

            try
            {
                // Check cache first
                var cachedData = await _cacheService.GetAsync<FundamentalData>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache HIT for fundamentals {Symbol}", symbol);
                    return cachedData;
                }

                _logger.LogInformation("Cache MISS for fundamentals {Symbol}. Fetching from API", symbol);

                // Fetch from API
                var fundamentalData = await _stockDataProvider.GetFundamentalsAsync(symbol, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, fundamentalData, TimeSpan.FromSeconds(FundamentalsTTL));
                
                // Store in stale cache with longer TTL for fallback (7 days)
                await _cacheService.SetAsync(staleCacheKey, fundamentalData, TimeSpan.FromDays(7));

                _logger.LogInformation("Cached fundamentals for {Symbol} with TTL {TTL}s", symbol, FundamentalsTTL);

                return fundamentalData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fundamentals for {Symbol}. Attempting fallback to stale cache", symbol);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<FundamentalData>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning("Serving STALE cached data for fundamentals {Symbol} due to API failure", symbol);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError("No cached data available for fundamentals {Symbol}. Unable to serve request", symbol);
                throw;
            }
        }

        /// <summary>
        /// Gets company profile for a symbol with caching
        /// Cache key: profile:{symbol}
        /// TTL: 7 days
        /// Fallback: Returns stale cached data if API fails
        /// </summary>
        public async Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"profile:{symbol}";
            var staleCacheKey = $"stale:{cacheKey}";

            try
            {
                // Check cache first
                var cachedData = await _cacheService.GetAsync<CompanyProfile>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache HIT for company profile {Symbol}", symbol);
                    return cachedData;
                }

                _logger.LogInformation("Cache MISS for company profile {Symbol}. Fetching from API", symbol);

                // Fetch from API
                var companyProfile = await _stockDataProvider.GetCompanyProfileAsync(symbol, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, companyProfile, TimeSpan.FromSeconds(ProfileTTL));
                
                // Store in stale cache with longer TTL for fallback (30 days)
                await _cacheService.SetAsync(staleCacheKey, companyProfile, TimeSpan.FromDays(30));

                _logger.LogInformation("Cached company profile for {Symbol} with TTL {TTL}s", symbol, ProfileTTL);

                return companyProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company profile for {Symbol}. Attempting fallback to stale cache", symbol);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<CompanyProfile>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning("Serving STALE cached data for company profile {Symbol} due to API failure", symbol);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError("No cached data available for company profile {Symbol}. Unable to serve request", symbol);
                throw;
            }
        }

        /// <summary>
        /// Searches for symbols with caching
        /// Cache key: search:{query}
        /// TTL: 1 hour
        /// Fallback: Returns stale cached data if API fails
        /// </summary>
        public async Task<List<StockSearchResult>> SearchSymbolsAsync(
            string query,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"search:{query.ToLowerInvariant()}";
            var staleCacheKey = $"stale:{cacheKey}";

            try
            {
                // Check cache first
                var cachedData = await _cacheService.GetAsync<List<StockSearchResult>>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache HIT for search query '{Query}'", query);
                    return cachedData;
                }

                _logger.LogInformation("Cache MISS for search query '{Query}'. Fetching from API", query);

                // Fetch from API
                var searchResults = await _stockDataProvider.SearchSymbolsAsync(query, limit, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, searchResults, TimeSpan.FromSeconds(SearchTTL));
                
                // Store in stale cache with longer TTL for fallback (7 days)
                await _cacheService.SetAsync(staleCacheKey, searchResults, TimeSpan.FromDays(7));

                _logger.LogInformation("Cached search results for '{Query}' with TTL {TTL}s", query, SearchTTL);

                return searchResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for '{Query}'. Attempting fallback to stale cache", query);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<List<StockSearchResult>>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning("Serving STALE cached data for search query '{Query}' due to API failure", query);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError("No cached data available for search query '{Query}'. Unable to serve request", query);
                throw;
            }
        }

        /// <summary>
        /// Warms the cache for frequently requested symbols
        /// This method should be called during off-peak hours or on application startup
        /// </summary>
        public async Task WarmCacheAsync(List<string> symbols, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting cache warming for {Count} symbols", symbols.Count);

            var tasks = symbols.Select(async symbol =>
            {
                try
                {
                    // Warm quote cache
                    await GetQuoteAsync(symbol, cancellationToken);
                    
                    // Warm company profile cache (changes infrequently)
                    await GetCompanyProfileAsync(symbol, cancellationToken);
                    
                    _logger.LogInformation("Successfully warmed cache for {Symbol}", symbol);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for {Symbol}", symbol);
                }
            });

            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Cache warming completed for {Count} symbols", symbols.Count);
        }
    }
}
