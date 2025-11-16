using Microsoft.Extensions.Logging;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.Configuration;

namespace StockSensePro.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IStockDataProvider _stockDataProvider;
        private readonly ICacheService _cacheService;
        private readonly CacheSettings _cacheSettings;
        private readonly ILogger<StockService> _logger;

        public StockService(
            IStockRepository stockRepository,
            IStockDataProvider stockDataProvider,
            ICacheService cacheService,
            CacheSettings cacheSettings,
            ILogger<StockService> logger)
        {
            _stockRepository = stockRepository;
            _stockDataProvider = stockDataProvider;
            _cacheService = cacheService;
            _cacheSettings = cacheSettings;
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
                    _logger.LogInformation(
                        "Service Cache HIT: DataType=Quote, Symbol={Symbol}, CacheKey={CacheKey}",
                        symbol,
                        cacheKey);
                    return cachedData;
                }

                _logger.LogInformation(
                    "Service Cache MISS: DataType=Quote, Symbol={Symbol}, CacheKey={CacheKey}, Action=FetchingFromAPI",
                    symbol,
                    cacheKey);

                // Fetch from API
                var marketData = await _stockDataProvider.GetQuoteAsync(symbol, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, marketData, TimeSpan.FromSeconds(_cacheSettings.QuoteTTL));
                
                // Store in stale cache with longer TTL for fallback (24 hours)
                await _cacheService.SetAsync(staleCacheKey, marketData, TimeSpan.FromHours(24));

                _logger.LogInformation(
                    "Service Cached: DataType=Quote, Symbol={Symbol}, CacheKey={CacheKey}, TTL={TTLSeconds}s, StaleTTL={StaleTTLHours}h",
                    symbol,
                    cacheKey,
                    _cacheSettings.QuoteTTL,
                    24);

                return marketData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Service Error: DataType=Quote, Symbol={Symbol}, ErrorType={ErrorType}, Action=AttemptingFallback",
                    symbol,
                    ex.GetType().Name);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<MarketData>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning(
                        "Service Fallback: DataType=Quote, Symbol={Symbol}, CacheKey={CacheKey}, Status=ServingStaleData",
                        symbol,
                        staleCacheKey);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError(
                    "Service Failure: DataType=Quote, Symbol={Symbol}, Status=NoFallbackAvailable",
                    symbol);
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
                    _logger.LogInformation(
                        "Service Cache HIT: DataType=HistoricalPrices, Symbol={Symbol}, DateRange={StartDate} to {EndDate}, Interval={Interval}, CacheKey={CacheKey}",
                        symbol,
                        startStr,
                        endStr,
                        interval,
                        cacheKey);
                    return cachedData;
                }

                _logger.LogInformation(
                    "Service Cache MISS: DataType=HistoricalPrices, Symbol={Symbol}, DateRange={StartDate} to {EndDate}, Interval={Interval}, CacheKey={CacheKey}, Action=FetchingFromAPI",
                    symbol,
                    startStr,
                    endStr,
                    interval,
                    cacheKey);

                // Fetch from API
                var historicalPrices = await _stockDataProvider.GetHistoricalPricesAsync(
                    symbol, startDate, endDate, interval, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, historicalPrices, TimeSpan.FromSeconds(_cacheSettings.HistoricalTTL));
                
                // Store in stale cache with longer TTL for fallback (7 days)
                await _cacheService.SetAsync(staleCacheKey, historicalPrices, TimeSpan.FromDays(7));

                _logger.LogInformation(
                    "Service Cached: DataType=HistoricalPrices, Symbol={Symbol}, RecordCount={Count}, CacheKey={CacheKey}, TTL={TTLSeconds}s, StaleTTL={StaleTTLDays}d",
                    symbol,
                    historicalPrices.Count,
                    cacheKey,
                    _cacheSettings.HistoricalTTL,
                    7);

                return historicalPrices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Service Error: DataType=HistoricalPrices, Symbol={Symbol}, ErrorType={ErrorType}, Action=AttemptingFallback",
                    symbol,
                    ex.GetType().Name);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<List<StockPrice>>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning(
                        "Service Fallback: DataType=HistoricalPrices, Symbol={Symbol}, CacheKey={CacheKey}, Status=ServingStaleData",
                        symbol,
                        staleCacheKey);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError(
                    "Service Failure: DataType=HistoricalPrices, Symbol={Symbol}, Status=NoFallbackAvailable",
                    symbol);
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
                    _logger.LogInformation(
                        "Service Cache HIT: DataType=Fundamentals, Symbol={Symbol}, CacheKey={CacheKey}",
                        symbol,
                        cacheKey);
                    return cachedData;
                }

                _logger.LogInformation(
                    "Service Cache MISS: DataType=Fundamentals, Symbol={Symbol}, CacheKey={CacheKey}, Action=FetchingFromAPI",
                    symbol,
                    cacheKey);

                // Fetch from API
                var fundamentalData = await _stockDataProvider.GetFundamentalsAsync(symbol, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, fundamentalData, TimeSpan.FromSeconds(_cacheSettings.FundamentalsTTL));
                
                // Store in stale cache with longer TTL for fallback (7 days)
                await _cacheService.SetAsync(staleCacheKey, fundamentalData, TimeSpan.FromDays(7));

                _logger.LogInformation(
                    "Service Cached: DataType=Fundamentals, Symbol={Symbol}, CacheKey={CacheKey}, TTL={TTLSeconds}s, StaleTTL={StaleTTLDays}d",
                    symbol,
                    cacheKey,
                    _cacheSettings.FundamentalsTTL,
                    7);

                return fundamentalData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Service Error: DataType=Fundamentals, Symbol={Symbol}, ErrorType={ErrorType}, Action=AttemptingFallback",
                    symbol,
                    ex.GetType().Name);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<FundamentalData>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning(
                        "Service Fallback: DataType=Fundamentals, Symbol={Symbol}, CacheKey={CacheKey}, Status=ServingStaleData",
                        symbol,
                        staleCacheKey);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError(
                    "Service Failure: DataType=Fundamentals, Symbol={Symbol}, Status=NoFallbackAvailable",
                    symbol);
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
                    _logger.LogInformation(
                        "Service Cache HIT: DataType=CompanyProfile, Symbol={Symbol}, CacheKey={CacheKey}",
                        symbol,
                        cacheKey);
                    return cachedData;
                }

                _logger.LogInformation(
                    "Service Cache MISS: DataType=CompanyProfile, Symbol={Symbol}, CacheKey={CacheKey}, Action=FetchingFromAPI",
                    symbol,
                    cacheKey);

                // Fetch from API
                var companyProfile = await _stockDataProvider.GetCompanyProfileAsync(symbol, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, companyProfile, TimeSpan.FromSeconds(_cacheSettings.ProfileTTL));
                
                // Store in stale cache with longer TTL for fallback (30 days)
                await _cacheService.SetAsync(staleCacheKey, companyProfile, TimeSpan.FromDays(30));

                _logger.LogInformation(
                    "Service Cached: DataType=CompanyProfile, Symbol={Symbol}, CacheKey={CacheKey}, TTL={TTLSeconds}s, StaleTTL={StaleTTLDays}d",
                    symbol,
                    cacheKey,
                    _cacheSettings.ProfileTTL,
                    30);

                return companyProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Service Error: DataType=CompanyProfile, Symbol={Symbol}, ErrorType={ErrorType}, Action=AttemptingFallback",
                    symbol,
                    ex.GetType().Name);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<CompanyProfile>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning(
                        "Service Fallback: DataType=CompanyProfile, Symbol={Symbol}, CacheKey={CacheKey}, Status=ServingStaleData",
                        symbol,
                        staleCacheKey);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError(
                    "Service Failure: DataType=CompanyProfile, Symbol={Symbol}, Status=NoFallbackAvailable",
                    symbol);
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
                    _logger.LogInformation(
                        "Service Cache HIT: DataType=Search, Query={Query}, CacheKey={CacheKey}",
                        query,
                        cacheKey);
                    return cachedData;
                }

                _logger.LogInformation(
                    "Service Cache MISS: DataType=Search, Query={Query}, CacheKey={CacheKey}, Action=FetchingFromAPI",
                    query,
                    cacheKey);

                // Fetch from API
                var searchResults = await _stockDataProvider.SearchSymbolsAsync(query, limit, cancellationToken);

                // Store in cache with normal TTL
                await _cacheService.SetAsync(cacheKey, searchResults, TimeSpan.FromSeconds(_cacheSettings.SearchTTL));
                
                // Store in stale cache with longer TTL for fallback (7 days)
                await _cacheService.SetAsync(staleCacheKey, searchResults, TimeSpan.FromDays(7));

                _logger.LogInformation(
                    "Service Cached: DataType=Search, Query={Query}, ResultCount={Count}, CacheKey={CacheKey}, TTL={TTLSeconds}s, StaleTTL={StaleTTLDays}d",
                    query,
                    searchResults.Count,
                    cacheKey,
                    _cacheSettings.SearchTTL,
                    7);

                return searchResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Service Error: DataType=Search, Query={Query}, ErrorType={ErrorType}, Action=AttemptingFallback",
                    query,
                    ex.GetType().Name);
                
                // Try to get stale cached data as fallback
                var staleData = await _cacheService.GetAsync<List<StockSearchResult>>(staleCacheKey);
                if (staleData != null)
                {
                    _logger.LogWarning(
                        "Service Fallback: DataType=Search, Query={Query}, CacheKey={CacheKey}, Status=ServingStaleData",
                        query,
                        staleCacheKey);
                    return staleData;
                }

                // No fallback available, rethrow
                _logger.LogError(
                    "Service Failure: DataType=Search, Query={Query}, Status=NoFallbackAvailable",
                    query);
                throw;
            }
        }

        /// <summary>
        /// Warms the cache for frequently requested symbols
        /// This method should be called during off-peak hours or on application startup
        /// </summary>
        public async Task WarmCacheAsync(List<string> symbols, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation(
                "Cache Warming Started: SymbolCount={Count}, Timestamp={Timestamp}",
                symbols.Count,
                startTime);

            var successCount = 0;
            var failureCount = 0;

            var tasks = symbols.Select(async symbol =>
            {
                try
                {
                    // Warm quote cache
                    await GetQuoteAsync(symbol, cancellationToken);
                    
                    // Warm company profile cache (changes infrequently)
                    await GetCompanyProfileAsync(symbol, cancellationToken);
                    
                    System.Threading.Interlocked.Increment(ref successCount);
                    
                    _logger.LogInformation(
                        "Cache Warming Success: Symbol={Symbol}",
                        symbol);
                }
                catch (Exception ex)
                {
                    System.Threading.Interlocked.Increment(ref failureCount);
                    
                    _logger.LogWarning(ex,
                        "Cache Warming Failed: Symbol={Symbol}, ErrorType={ErrorType}",
                        symbol,
                        ex.GetType().Name);
                }
            });

            await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Cache Warming Completed: TotalSymbols={Total}, SuccessCount={Success}, FailureCount={Failure}, Duration={DurationMs}ms",
                symbols.Count,
                successCount,
                failureCount,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
