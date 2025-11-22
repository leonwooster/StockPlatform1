using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSensePro.Core.Configuration;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Exceptions;
using StockSensePro.Core.Interfaces;
using StockSensePro.Infrastructure.Models;

namespace StockSensePro.Infrastructure.Services;

/// <summary>
/// Alpha Vantage implementation of IStockDataProvider
/// Provides stock market data from Alpha Vantage API
/// </summary>
public class AlphaVantageService : IStockDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlphaVantageService> _logger;
    private readonly AlphaVantageSettings _settings;
    private readonly CacheSettings _cacheSettings;
    private readonly ICacheService? _cacheService;
    private readonly IStockDataProvider? _yahooFinanceProvider;
    private readonly IAlphaVantageRateLimiter? _rateLimiter;

    public AlphaVantageService(
        HttpClient httpClient,
        ILogger<AlphaVantageService> logger,
        IOptions<AlphaVantageSettings> settings,
        IOptions<CacheSettings> cacheSettings,
        ICacheService? cacheService = null,
        IStockDataProvider? yahooFinanceProvider = null,
        IAlphaVantageRateLimiter? rateLimiter = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _cacheSettings = cacheSettings?.Value ?? throw new ArgumentNullException(nameof(cacheSettings));
        _cacheService = cacheService;
        _yahooFinanceProvider = yahooFinanceProvider;
        _rateLimiter = rateLimiter;

        // Configure HttpClient with base URL and timeout
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Timeout);

        _logger.LogInformation(
            "AlphaVantageService initialized: BaseUrl={BaseUrl}, Timeout={Timeout}s, Enabled={Enabled}, CacheEnabled={CacheEnabled}, EnrichmentEnabled={EnrichmentEnabled}, RateLimiterEnabled={RateLimiterEnabled}, CacheTTLs={{Quote={QuoteTTL}s, Historical={HistoricalTTL}s, Fundamentals={FundamentalsTTL}s, Profile={ProfileTTL}s, Search={SearchTTL}s}}",
            _settings.BaseUrl,
            _settings.Timeout,
            _settings.Enabled,
            _cacheService != null,
            _settings.DataEnrichment.EnableBidAskEnrichment || _settings.DataEnrichment.EnableCalculated52WeekRange || _settings.DataEnrichment.EnableCalculatedAverageVolume,
            _rateLimiter != null,
            _cacheSettings.AlphaVantage.QuoteTTL,
            _cacheSettings.AlphaVantage.HistoricalTTL,
            _cacheSettings.AlphaVantage.FundamentalsTTL,
            _cacheSettings.AlphaVantage.ProfileTTL,
            _cacheSettings.AlphaVantage.SearchTTL);
    }

    /// <summary>
    /// Helper method to execute HTTP requests with structured logging, rate limiting, and retry logic
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteRequestWithLoggingAsync(
        string endpoint,
        string symbol,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Check rate limit before making the request
        if (_rateLimiter != null)
        {
            var rateLimitStatus = _rateLimiter.GetStatus();
            _logger.LogDebug(
                "Rate limit status before request: Endpoint={Endpoint}, Symbol={Symbol}, MinuteRemaining={MinuteRemaining}/{MinuteLimit}, DayRemaining={DayRemaining}/{DayLimit}",
                endpoint,
                symbol,
                rateLimitStatus.MinuteRequestsRemaining,
                rateLimitStatus.MinuteRequestsLimit,
                rateLimitStatus.DayRequestsRemaining,
                rateLimitStatus.DayRequestsLimit);

            // Try to acquire rate limit token
            var acquired = await _rateLimiter.TryAcquireAsync(cancellationToken);
            
            if (!acquired)
            {
                // Rate limit exceeded - log the event
                var status = _rateLimiter.GetStatus();
                _logger.LogWarning(
                    "Rate limit exceeded for Alpha Vantage API: Endpoint={Endpoint}, Symbol={Symbol}, MinuteRemaining={MinuteRemaining}, DayRemaining={DayRemaining}, MinuteResetIn={MinuteResetIn}, DayResetIn={DayResetIn}",
                    endpoint,
                    symbol,
                    status.MinuteRequestsRemaining,
                    status.DayRequestsRemaining,
                    status.MinuteWindowResetIn,
                    status.DayWindowResetIn);

                // Throw rate limit exception to trigger cache fallback
                throw new RateLimitExceededException(
                    $"Alpha Vantage rate limit exceeded. Minute remaining: {status.MinuteRequestsRemaining}, Day remaining: {status.DayRequestsRemaining}",
                    symbol);
            }

            _logger.LogDebug(
                "Rate limit token acquired for request: Endpoint={Endpoint}, Symbol={Symbol}",
                endpoint,
                symbol);
        }

        // Add API key to parameters
        parameters["apikey"] = _settings.ApiKey;

        // Build query string
        var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var requestUrl = $"?{queryString}";

        // Implement retry logic with exponential backoff
        var maxRetries = _settings.MaxRetries;
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                if (retryCount > 0)
                {
                    // Calculate exponential backoff delay: 2^retryCount * 100ms
                    var delayMs = (int)Math.Pow(2, retryCount) * 100;
                    _logger.LogInformation(
                        "Retrying Alpha Vantage API request (Attempt {Attempt}/{MaxRetries}): Endpoint={Endpoint}, Symbol={Symbol}, DelayMs={DelayMs}",
                        retryCount + 1,
                        maxRetries + 1,
                        endpoint,
                        symbol,
                        delayMs);
                    
                    await Task.Delay(delayMs, cancellationToken);
                }

                _logger.LogInformation(
                    "Alpha Vantage API Request: Endpoint={Endpoint}, Symbol={Symbol}, Attempt={Attempt}, Timestamp={Timestamp}, BaseUrl={BaseUrl}",
                    endpoint,
                    symbol,
                    retryCount + 1,
                    startTime,
                    _httpClient.BaseAddress);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Alpha Vantage API Response: Endpoint={Endpoint}, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms, Attempt={Attempt}, Timestamp={Timestamp}",
                    endpoint,
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    retryCount + 1,
                    DateTime.UtcNow);

                return response;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries)
            {
                lastException = ex;
                retryCount++;
                stopwatch.Stop();
                
                _logger.LogWarning(ex,
                    "Alpha Vantage API Request Failed (Transient Network Error): Endpoint={Endpoint}, Symbol={Symbol}, Attempt={Attempt}/{MaxRetries}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, WillRetry=true",
                    endpoint,
                    symbol,
                    retryCount,
                    maxRetries + 1,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name);
                
                stopwatch.Restart();
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && retryCount < maxRetries)
            {
                // Timeout occurred, not user cancellation
                lastException = ex;
                retryCount++;
                stopwatch.Stop();
                
                _logger.LogWarning(ex,
                    "Alpha Vantage API Request Timeout: Endpoint={Endpoint}, Symbol={Symbol}, Attempt={Attempt}/{MaxRetries}, ResponseTime={ResponseTimeMs}ms, WillRetry=true",
                    endpoint,
                    symbol,
                    retryCount,
                    maxRetries + 1,
                    stopwatch.ElapsedMilliseconds);
                
                stopwatch.Restart();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Alpha Vantage API Request Failed (Non-Retryable Error): Endpoint={Endpoint}, Symbol={Symbol}, Attempt={Attempt}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}",
                    endpoint,
                    symbol,
                    retryCount + 1,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name);
                throw;
            }
        }

        // All retries exhausted
        stopwatch.Stop();
        _logger.LogError(lastException,
            "Alpha Vantage API Request Failed After All Retries: Endpoint={Endpoint}, Symbol={Symbol}, TotalAttempts={TotalAttempts}, TotalTime={TotalTimeMs}ms",
            endpoint,
            symbol,
            retryCount,
            stopwatch.ElapsedMilliseconds);
        
        throw lastException ?? new HttpRequestException("Request failed after all retries");
    }

    public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"alphavantage:quote:{symbol}";
        MarketData? cachedData = null;
        
        if (_cacheService != null)
        {
            cachedData = await _cacheService.GetAsync<MarketData>(cacheKey);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["function"] = "GLOBAL_QUOTE",
                ["symbol"] = symbol
            };

            var response = await ExecuteRequestWithLoggingAsync("GLOBAL_QUOTE", symbol, parameters, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Validate response structure
            ValidateResponseStructure(json, "GLOBAL_QUOTE", symbol);

            // Check for error responses (Alpha Vantage returns 200 OK even for errors)
            if (!response.IsSuccessStatusCode || 
                json.Contains("Invalid API call", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("frequency", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Note", StringComparison.OrdinalIgnoreCase))
            {
                await HandleErrorResponseAsync(response, symbol, json, cancellationToken);
            }

            var result = JsonSerializer.Deserialize<AlphaVantageGlobalQuoteResponse>(json);

            if (result?.GlobalQuote == null || string.IsNullOrEmpty(result.GlobalQuote.Symbol))
            {
                _logger.LogError(
                    "Invalid response structure from Alpha Vantage: Missing or empty GlobalQuote. Symbol={Symbol}, ResponseLength={Length}",
                    symbol,
                    json.Length);
                throw new SymbolNotFoundException(symbol);
            }

            var marketData = MapToMarketData(result.GlobalQuote, symbol);

            // Apply data enrichment if enabled
            await EnrichMarketDataAsync(marketData, symbol, cancellationToken);

            // Cache the successful result with provider-specific TTL
            if (_cacheService != null)
            {
                var ttl = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.QuoteTTL);
                await _cacheService.SetAsync(cacheKey, marketData, ttl);
                
                _logger.LogDebug(
                    "Cached quote for {Symbol} with TTL {TTL}s",
                    symbol,
                    _cacheSettings.AlphaVantage.QuoteTTL);
            }

            _logger.LogInformation(
                "Successfully fetched quote for {Symbol}: Price={Price}, Change={ChangePercent}%",
                symbol,
                marketData.CurrentPrice,
                marketData.ChangePercent);

            return marketData;
        }
        catch (SymbolNotFoundException)
        {
            throw;
        }
        catch (RateLimitExceededException ex)
        {
            // Return cached data if available when rate limited
            if (cachedData != null)
            {
                var cacheAge = DateTime.UtcNow - cachedData.Timestamp;
                _logger.LogWarning(
                    "Rate limit exceeded for {Symbol}, returning cached data (Age: {CacheAge} minutes). Consider upgrading Alpha Vantage plan or implementing request throttling.",
                    symbol,
                    cacheAge.TotalMinutes);
                return cachedData;
            }

            _logger.LogError(ex, 
                "Rate limit exceeded for {Symbol} and no cached data available. Request will fail. Consider implementing a retry queue or upgrading Alpha Vantage plan.",
                symbol);
            throw;
        }
        catch (ApiUnavailableException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while fetching quote for {Symbol}", symbol);
            throw new ApiUnavailableException($"Network error while fetching quote for {symbol}", symbol, ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while fetching quote for {Symbol}", symbol);
            throw new ApiUnavailableException($"Request timeout while fetching quote for {symbol}", symbol, ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Alpha Vantage response for {Symbol}", symbol);
            throw new ApiUnavailableException($"Failed to parse Alpha Vantage response for {symbol}", symbol, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching quote for {Symbol}", symbol);
            throw new ApiUnavailableException($"Unexpected error while fetching quote for {symbol}", symbol, ex);
        }
    }

    public async Task<List<MarketData>> GetQuotesAsync(List<string> symbols, CancellationToken cancellationToken = default)
    {
        if (symbols == null || symbols.Count == 0)
        {
            _logger.LogWarning("GetQuotesAsync called with empty symbol list");
            return new List<MarketData>();
        }

        _logger.LogInformation(
            "Fetching quotes for {Count} symbols: {Symbols}",
            symbols.Count,
            string.Join(", ", symbols));

        var results = new List<MarketData>();
        var errors = new List<string>();

        // Alpha Vantage doesn't support batch requests in the free tier
        // We need to make individual requests for each symbol
        // This respects rate limits by making sequential requests
        foreach (var symbol in symbols)
        {
            try
            {
                var quote = await GetQuoteAsync(symbol, cancellationToken);
                results.Add(quote);
            }
            catch (SymbolNotFoundException)
            {
                _logger.LogWarning("Symbol {Symbol} not found, skipping", symbol);
                errors.Add($"{symbol}: Not found");
            }
            catch (RateLimitExceededException)
            {
                _logger.LogWarning("Rate limit exceeded while fetching {Symbol}, stopping batch request", symbol);
                errors.Add($"{symbol}: Rate limit exceeded");
                // Stop processing remaining symbols when rate limit is hit
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch quote for {Symbol}, continuing with remaining symbols", symbol);
                errors.Add($"{symbol}: {ex.Message}");
                // Continue with other symbols on individual failures
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Batch quote request completed with {SuccessCount} successes and {ErrorCount} errors. Errors: {Errors}",
                results.Count,
                errors.Count,
                string.Join("; ", errors));
        }
        else
        {
            _logger.LogInformation(
                "Successfully fetched all {Count} quotes",
                results.Count);
        }

        return results;
    }

    public async Task<List<StockPrice>> GetHistoricalPricesAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        TimeInterval interval = TimeInterval.Daily,
        CancellationToken cancellationToken = default)
    {
        // Validate date range
        if (startDate > endDate)
        {
            _logger.LogWarning(
                "Invalid date range for {Symbol}: StartDate={StartDate} is after EndDate={EndDate}",
                symbol,
                startDate,
                endDate);
            throw new ArgumentException("Start date must be before or equal to end date", nameof(startDate));
        }

        if (endDate > DateTime.UtcNow.Date)
        {
            _logger.LogWarning(
                "End date {EndDate} is in the future for {Symbol}, adjusting to today",
                endDate,
                symbol);
            endDate = DateTime.UtcNow.Date;
        }

        // Check cache first
        var cacheKey = $"alphavantage:historical:{symbol}:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}:{interval}";
        List<StockPrice>? cachedData = null;
        
        if (_cacheService != null)
        {
            cachedData = await _cacheService.GetAsync<List<StockPrice>>(cacheKey);
        }

        try
        {
            // Determine the Alpha Vantage function based on interval
            var function = interval switch
            {
                TimeInterval.Daily => "TIME_SERIES_DAILY",
                TimeInterval.Weekly => "TIME_SERIES_WEEKLY",
                TimeInterval.Monthly => "TIME_SERIES_MONTHLY",
                _ => throw new ArgumentException($"Unsupported time interval: {interval}", nameof(interval))
            };

            var parameters = new Dictionary<string, string>
            {
                ["function"] = function,
                ["symbol"] = symbol,
                ["outputsize"] = "full" // Get full history (up to 20 years)
            };

            var response = await ExecuteRequestWithLoggingAsync(function, symbol, parameters, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Validate response structure
            ValidateResponseStructure(json, function, symbol);

            // Check for error responses
            if (!response.IsSuccessStatusCode ||
                json.Contains("Invalid API call", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("frequency", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Note", StringComparison.OrdinalIgnoreCase))
            {
                await HandleErrorResponseAsync(response, symbol, json, cancellationToken);
            }

            var result = JsonSerializer.Deserialize<AlphaVantageTimeSeriesResponse>(json);

            if (result?.TimeSeries == null || result.TimeSeries.Count == 0)
            {
                _logger.LogError(
                    "Invalid response structure from Alpha Vantage: Missing or empty TimeSeries. Symbol={Symbol}, Interval={Interval}, ResponseLength={Length}",
                    symbol,
                    interval,
                    json.Length);
                throw new SymbolNotFoundException(symbol);
            }

            // Map to StockPrice entities and filter by date range
            var stockPrices = result.TimeSeries
                .Select(kvp => MapToStockPrice(kvp.Key, kvp.Value, symbol))
                .Where(sp => sp.Date >= startDate.Date && sp.Date <= endDate.Date)
                .OrderBy(sp => sp.Date)
                .ToList();

            // Cache the successful result with provider-specific TTL
            if (_cacheService != null)
            {
                var ttl = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.HistoricalTTL);
                await _cacheService.SetAsync(cacheKey, stockPrices, ttl);
                
                _logger.LogDebug(
                    "Cached historical prices for {Symbol} with TTL {TTL}s",
                    symbol,
                    _cacheSettings.AlphaVantage.HistoricalTTL);
            }

            _logger.LogInformation(
                "Successfully fetched {Count} historical prices for {Symbol} from {StartDate} to {EndDate} with {Interval} interval",
                stockPrices.Count,
                symbol,
                startDate.Date,
                endDate.Date,
                interval);

            return stockPrices;
        }
        catch (SymbolNotFoundException)
        {
            throw;
        }
        catch (RateLimitExceededException ex)
        {
            // Return cached data if available when rate limited
            if (cachedData != null)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for {Symbol} historical data, returning {Count} cached prices. Consider upgrading Alpha Vantage plan or implementing request throttling.",
                    symbol,
                    cachedData.Count);
                return cachedData;
            }

            _logger.LogError(ex, 
                "Rate limit exceeded for {Symbol} historical data and no cached data available. Request will fail. Consider implementing a retry queue or upgrading Alpha Vantage plan.",
                symbol);
            throw;
        }
        catch (ApiUnavailableException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while fetching historical prices for {Symbol}", symbol);
            throw new ApiUnavailableException($"Network error while fetching historical prices for {symbol}", symbol, ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while fetching historical prices for {Symbol}", symbol);
            throw new ApiUnavailableException($"Request timeout while fetching historical prices for {symbol}", symbol, ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Alpha Vantage historical data response for {Symbol}", symbol);
            throw new ApiUnavailableException($"Failed to parse Alpha Vantage historical data response for {symbol}", symbol, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching historical prices for {Symbol}", symbol);
            throw new ApiUnavailableException($"Unexpected error while fetching historical prices for {symbol}", symbol, ex);
        }
    }

    public async Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"alphavantage:fundamentals:{symbol}";
        FundamentalData? cachedData = null;
        
        if (_cacheService != null)
        {
            cachedData = await _cacheService.GetAsync<FundamentalData>(cacheKey);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["function"] = "OVERVIEW",
                ["symbol"] = symbol
            };

            var response = await ExecuteRequestWithLoggingAsync("OVERVIEW", symbol, parameters, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Validate response structure
            ValidateResponseStructure(json, "OVERVIEW", symbol);

            // Check for error responses
            if (!response.IsSuccessStatusCode ||
                json.Contains("Invalid API call", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("frequency", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Note", StringComparison.OrdinalIgnoreCase))
            {
                await HandleErrorResponseAsync(response, symbol, json, cancellationToken);
            }

            var result = JsonSerializer.Deserialize<AlphaVantageCompanyOverviewResponse>(json);

            if (result == null || string.IsNullOrEmpty(result.Symbol))
            {
                _logger.LogError(
                    "Invalid response structure from Alpha Vantage: Missing or empty company overview. Symbol={Symbol}, ResponseLength={Length}",
                    symbol,
                    json.Length);
                throw new SymbolNotFoundException(symbol);
            }

            var fundamentalData = MapToFundamentalData(result, symbol);

            // Cache the successful result with provider-specific TTL
            if (_cacheService != null)
            {
                var ttl = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.FundamentalsTTL);
                await _cacheService.SetAsync(cacheKey, fundamentalData, ttl);
                
                _logger.LogDebug(
                    "Cached fundamentals for {Symbol} with TTL {TTL}s",
                    symbol,
                    _cacheSettings.AlphaVantage.FundamentalsTTL);
            }

            _logger.LogInformation(
                "Successfully fetched fundamentals for {Symbol}: PE={PERatio}, EPS={EPS}, MarketCap={MarketCap}",
                symbol,
                fundamentalData.PERatio,
                fundamentalData.EPS,
                result.MarketCapitalization);

            return fundamentalData;
        }
        catch (SymbolNotFoundException)
        {
            throw;
        }
        catch (RateLimitExceededException ex)
        {
            // Return cached data if available when rate limited
            if (cachedData != null)
            {
                var cacheAge = DateTime.UtcNow - cachedData.LastUpdated;
                _logger.LogWarning(
                    "Rate limit exceeded for {Symbol} fundamentals, returning cached data (Age: {CacheAge} hours). Consider upgrading Alpha Vantage plan or implementing request throttling.",
                    symbol,
                    cacheAge.TotalHours);
                return cachedData;
            }

            _logger.LogError(ex, 
                "Rate limit exceeded for {Symbol} fundamentals and no cached data available. Request will fail. Consider implementing a retry queue or upgrading Alpha Vantage plan.",
                symbol);
            throw;
        }
        catch (ApiUnavailableException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while fetching fundamentals for {Symbol}", symbol);
            throw new ApiUnavailableException($"Network error while fetching fundamentals for {symbol}", symbol, ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while fetching fundamentals for {Symbol}", symbol);
            throw new ApiUnavailableException($"Request timeout while fetching fundamentals for {symbol}", symbol, ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Alpha Vantage fundamentals response for {Symbol}", symbol);
            throw new ApiUnavailableException($"Failed to parse Alpha Vantage fundamentals response for {symbol}", symbol, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching fundamentals for {Symbol}", symbol);
            throw new ApiUnavailableException($"Unexpected error while fetching fundamentals for {symbol}", symbol, ex);
        }
    }

    public async Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"alphavantage:profile:{symbol}";
        CompanyProfile? cachedData = null;
        
        if (_cacheService != null)
        {
            cachedData = await _cacheService.GetAsync<CompanyProfile>(cacheKey);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["function"] = "OVERVIEW",
                ["symbol"] = symbol
            };

            var response = await ExecuteRequestWithLoggingAsync("OVERVIEW", symbol, parameters, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Validate response structure
            ValidateResponseStructure(json, "OVERVIEW", symbol);

            // Check for error responses
            if (!response.IsSuccessStatusCode ||
                json.Contains("Invalid API call", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("frequency", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Note", StringComparison.OrdinalIgnoreCase))
            {
                await HandleErrorResponseAsync(response, symbol, json, cancellationToken);
            }

            var result = JsonSerializer.Deserialize<AlphaVantageCompanyOverviewResponse>(json);

            if (result == null || string.IsNullOrEmpty(result.Symbol))
            {
                _logger.LogError(
                    "Invalid response structure from Alpha Vantage: Missing or empty company profile. Symbol={Symbol}, ResponseLength={Length}",
                    symbol,
                    json.Length);
                throw new SymbolNotFoundException(symbol);
            }

            var companyProfile = MapToCompanyProfile(result, symbol);

            // Cache the successful result with provider-specific TTL
            if (_cacheService != null)
            {
                var ttl = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.ProfileTTL);
                await _cacheService.SetAsync(cacheKey, companyProfile, ttl);
                
                _logger.LogDebug(
                    "Cached company profile for {Symbol} with TTL {TTL}s",
                    symbol,
                    _cacheSettings.AlphaVantage.ProfileTTL);
            }

            _logger.LogInformation(
                "Successfully fetched company profile for {Symbol}: Name={Name}, Sector={Sector}, Industry={Industry}",
                symbol,
                companyProfile.CompanyName,
                companyProfile.Sector,
                companyProfile.Industry);

            return companyProfile;
        }
        catch (SymbolNotFoundException)
        {
            throw;
        }
        catch (RateLimitExceededException ex)
        {
            // Return cached data if available when rate limited
            if (cachedData != null)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for {Symbol} company profile, returning cached data. Consider upgrading Alpha Vantage plan or implementing request throttling.",
                    symbol);
                return cachedData;
            }

            _logger.LogError(ex, 
                "Rate limit exceeded for {Symbol} company profile and no cached data available. Request will fail. Consider implementing a retry queue or upgrading Alpha Vantage plan.",
                symbol);
            throw;
        }
        catch (ApiUnavailableException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while fetching company profile for {Symbol}", symbol);
            throw new ApiUnavailableException($"Network error while fetching company profile for {symbol}", symbol, ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while fetching company profile for {Symbol}", symbol);
            throw new ApiUnavailableException($"Request timeout while fetching company profile for {symbol}", symbol, ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Alpha Vantage company profile response for {Symbol}", symbol);
            throw new ApiUnavailableException($"Failed to parse Alpha Vantage company profile response for {symbol}", symbol, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching company profile for {Symbol}", symbol);
            throw new ApiUnavailableException($"Unexpected error while fetching company profile for {symbol}", symbol, ex);
        }
    }

    public async Task<List<StockSearchResult>> SearchSymbolsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("SearchSymbolsAsync called with empty query");
            return new List<StockSearchResult>();
        }

        // Check cache first
        var cacheKey = $"alphavantage:search:{query}:{limit}";
        List<StockSearchResult>? cachedData = null;
        
        if (_cacheService != null)
        {
            cachedData = await _cacheService.GetAsync<List<StockSearchResult>>(cacheKey);
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["function"] = "SYMBOL_SEARCH",
                ["keywords"] = query
            };

            var response = await ExecuteRequestWithLoggingAsync("SYMBOL_SEARCH", query, parameters, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Validate response structure
            ValidateResponseStructure(json, "SYMBOL_SEARCH", query);

            // Check for error responses
            if (!response.IsSuccessStatusCode ||
                json.Contains("Invalid API call", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("frequency", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Note", StringComparison.OrdinalIgnoreCase))
            {
                await HandleErrorResponseAsync(response, query, json, cancellationToken);
            }

            var result = JsonSerializer.Deserialize<AlphaVantageSymbolSearchResponse>(json);

            if (result?.BestMatches == null || result.BestMatches.Count == 0)
            {
                _logger.LogInformation("No search results found for query: {Query}", query);
                return new List<StockSearchResult>();
            }

            // Map to StockSearchResult entities and limit results
            var searchResults = result.BestMatches
                .Take(limit)
                .Select(match => MapToStockSearchResult(match))
                .ToList();

            // Cache the successful result with provider-specific TTL
            if (_cacheService != null)
            {
                var ttl = TimeSpan.FromSeconds(_cacheSettings.AlphaVantage.SearchTTL);
                await _cacheService.SetAsync(cacheKey, searchResults, ttl);
                
                _logger.LogDebug(
                    "Cached search results for query '{Query}' with TTL {TTL}s",
                    query,
                    _cacheSettings.AlphaVantage.SearchTTL);
            }

            _logger.LogInformation(
                "Successfully found {Count} search results for query: {Query}",
                searchResults.Count,
                query);

            return searchResults;
        }
        catch (RateLimitExceededException ex)
        {
            // Return cached data if available when rate limited
            if (cachedData != null)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for search query '{Query}', returning {Count} cached results. Consider upgrading Alpha Vantage plan or implementing request throttling.",
                    query,
                    cachedData.Count);
                return cachedData;
            }

            _logger.LogError(ex, 
                "Rate limit exceeded for search query '{Query}' and no cached data available. Request will fail. Consider implementing a retry queue or upgrading Alpha Vantage plan.",
                query);
            throw;
        }
        catch (ApiUnavailableException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while searching for symbols with query: {Query}", query);
            throw new ApiUnavailableException($"Network error while searching for symbols with query: {query}", query, ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while searching for symbols with query: {Query}", query);
            throw new ApiUnavailableException($"Request timeout while searching for symbols with query: {query}", query, ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Alpha Vantage symbol search response for query: {Query}", query);
            throw new ApiUnavailableException($"Failed to parse Alpha Vantage symbol search response for query: {query}", query, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while searching for symbols with query: {Query}", query);
            throw new ApiUnavailableException($"Unexpected error while searching for symbols with query: {query}", query, ex);
        }
    }

    /// <summary>
    /// Calculates 52-week high and low from historical data
    /// </summary>
    private async Task<(decimal? High, decimal? Low)> Calculate52WeekRangeAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating 52-week high/low for {Symbol}", symbol);

            // Fetch 1 year of historical data
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddYears(-1);

            var historicalPrices = await GetHistoricalPricesAsync(
                symbol,
                startDate,
                endDate,
                TimeInterval.Daily,
                cancellationToken);

            if (historicalPrices == null || historicalPrices.Count == 0)
            {
                _logger.LogWarning("No historical data available to calculate 52-week range for {Symbol}", symbol);
                return (null, null);
            }

            var high = historicalPrices.Max(p => p.High);
            var low = historicalPrices.Min(p => p.Low);

            _logger.LogInformation(
                "Calculated 52-week range for {Symbol}: High={High}, Low={Low}",
                symbol,
                high,
                low);

            return (high, low);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate 52-week range for {Symbol}", symbol);
            return (null, null);
        }
    }

    /// <summary>
    /// Calculates average volume from the last 30 days of historical data
    /// </summary>
    private async Task<long?> CalculateAverageVolumeAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating average volume for {Symbol}", symbol);

            // Fetch 30 days of historical data
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-30);

            var historicalPrices = await GetHistoricalPricesAsync(
                symbol,
                startDate,
                endDate,
                TimeInterval.Daily,
                cancellationToken);

            if (historicalPrices == null || historicalPrices.Count == 0)
            {
                _logger.LogWarning("No historical data available to calculate average volume for {Symbol}", symbol);
                return null;
            }

            var averageVolume = (long)historicalPrices.Average(p => p.Volume);

            _logger.LogInformation(
                "Calculated average volume for {Symbol}: {AverageVolume} (based on {Days} days)",
                symbol,
                averageVolume,
                historicalPrices.Count);

            return averageVolume;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate average volume for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Enriches market data with bid/ask prices from Yahoo Finance
    /// </summary>
    private async Task<(decimal? BidPrice, decimal? AskPrice)> EnrichWithYahooFinanceBidAskAsync(
        string symbol,
        IStockDataProvider? yahooFinanceProvider,
        CancellationToken cancellationToken = default)
    {
        if (yahooFinanceProvider == null)
        {
            _logger.LogDebug("Yahoo Finance provider not available for bid/ask enrichment");
            return (null, null);
        }

        try
        {
            _logger.LogInformation("Enriching {Symbol} with Yahoo Finance bid/ask prices", symbol);

            var yahooData = await yahooFinanceProvider.GetQuoteAsync(symbol, cancellationToken);

            _logger.LogInformation(
                "Successfully enriched {Symbol} with Yahoo Finance bid/ask: Bid={Bid}, Ask={Ask}",
                symbol,
                yahooData.BidPrice,
                yahooData.AskPrice);

            return (yahooData.BidPrice, yahooData.AskPrice);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich {Symbol} with Yahoo Finance bid/ask prices, continuing without enrichment", symbol);
            return (null, null);
        }
    }

    /// <summary>
    /// Enriches market data with calculated fields and hybrid data from other providers
    /// </summary>
    private async Task EnrichMarketDataAsync(
        MarketData marketData,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var enrichmentTasks = new List<Task>();

        // Check cache for calculated fields first
        CalculatedFields? cachedFields = null;
        if (_cacheService != null)
        {
            var cacheKey = $"alphavantage:calculated:{symbol}";
            cachedFields = await _cacheService.GetAsync<CalculatedFields>(cacheKey);
        }

        // Enrich with bid/ask from Yahoo Finance if enabled
        if (_settings.DataEnrichment.EnableBidAskEnrichment)
        {
            _logger.LogDebug("Bid/ask enrichment enabled for {Symbol}", symbol);
            var bidAskTask = EnrichWithYahooFinanceBidAskAsync(symbol, _yahooFinanceProvider, cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        marketData.BidPrice = t.Result.BidPrice;
                        marketData.AskPrice = t.Result.AskPrice;
                    }
                }, cancellationToken);
            enrichmentTasks.Add(bidAskTask);
        }

        // Enrich with 52-week range if enabled
        if (_settings.DataEnrichment.EnableCalculated52WeekRange)
        {
            if (cachedFields != null && cachedFields.FiftyTwoWeekHigh.HasValue && cachedFields.FiftyTwoWeekLow.HasValue)
            {
                _logger.LogDebug("Using cached 52-week range for {Symbol}", symbol);
                marketData.FiftyTwoWeekHigh = cachedFields.FiftyTwoWeekHigh;
                marketData.FiftyTwoWeekLow = cachedFields.FiftyTwoWeekLow;
            }
            else
            {
                _logger.LogDebug("52-week range calculation enabled for {Symbol}", symbol);
                var rangeTask = Calculate52WeekRangeAsync(symbol, cancellationToken)
                    .ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            marketData.FiftyTwoWeekHigh = t.Result.High;
                            marketData.FiftyTwoWeekLow = t.Result.Low;
                            
                            // Update cache
                            if (_cacheService != null && cachedFields == null)
                            {
                                cachedFields = new CalculatedFields { Symbol = symbol };
                            }
                            if (cachedFields != null)
                            {
                                cachedFields.FiftyTwoWeekHigh = t.Result.High;
                                cachedFields.FiftyTwoWeekLow = t.Result.Low;
                            }
                        }
                    }, cancellationToken);
                enrichmentTasks.Add(rangeTask);
            }
        }

        // Enrich with average volume if enabled
        if (_settings.DataEnrichment.EnableCalculatedAverageVolume)
        {
            if (cachedFields != null && cachedFields.AverageVolume.HasValue)
            {
                _logger.LogDebug("Using cached average volume for {Symbol}", symbol);
                marketData.AverageVolume = cachedFields.AverageVolume;
            }
            else
            {
                _logger.LogDebug("Average volume calculation enabled for {Symbol}", symbol);
                var volumeTask = CalculateAverageVolumeAsync(symbol, cancellationToken)
                    .ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            marketData.AverageVolume = t.Result;
                            
                            // Update cache
                            if (_cacheService != null && cachedFields == null)
                            {
                                cachedFields = new CalculatedFields { Symbol = symbol };
                            }
                            if (cachedFields != null)
                            {
                                cachedFields.AverageVolume = t.Result;
                            }
                        }
                    }, cancellationToken);
                enrichmentTasks.Add(volumeTask);
            }
        }

        // Wait for all enrichment tasks to complete
        if (enrichmentTasks.Count > 0)
        {
            await Task.WhenAll(enrichmentTasks);
            
            // Save calculated fields to cache with provider-specific TTL
            if (_cacheService != null && cachedFields != null && 
                (cachedFields.FiftyTwoWeekHigh.HasValue || cachedFields.FiftyTwoWeekLow.HasValue || cachedFields.AverageVolume.HasValue))
            {
                var ttl = _cacheSettings.AlphaVantage.CalculatedFieldsTTL;
                cachedFields.CalculatedAt = DateTime.UtcNow;
                cachedFields.ExpiresAt = DateTime.UtcNow.AddSeconds(ttl);
                
                var cacheKey = $"alphavantage:calculated:{symbol}";
                await _cacheService.SetAsync(
                    cacheKey,
                    cachedFields,
                    TimeSpan.FromSeconds(ttl));
                
                _logger.LogDebug(
                    "Cached calculated fields for {Symbol} with TTL {TTL}s",
                    symbol,
                    ttl);
            }
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Alpha Vantage health check");

            // Use a lightweight API call to test connectivity
            // We'll use the SYMBOL_SEARCH endpoint with a simple query
            var parameters = new Dictionary<string, string>
            {
                ["function"] = "SYMBOL_SEARCH",
                ["keywords"] = "IBM" // Use a well-known symbol for testing
            };

            var response = await ExecuteRequestWithLoggingAsync("SYMBOL_SEARCH", "HEALTH_CHECK", parameters, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Check if the response is successful and doesn't contain error messages
            var isHealthy = response.IsSuccessStatusCode &&
                           !json.Contains("Invalid API call", StringComparison.OrdinalIgnoreCase) &&
                           !json.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase) &&
                           !string.IsNullOrWhiteSpace(json) &&
                           json.Contains("bestMatches", StringComparison.OrdinalIgnoreCase);

            if (isHealthy)
            {
                _logger.LogInformation("Alpha Vantage health check passed: API is accessible and responding correctly");
            }
            else
            {
                _logger.LogWarning(
                    "Alpha Vantage health check failed: StatusCode={StatusCode}, ResponseContainsData={HasData}",
                    response.StatusCode,
                    !string.IsNullOrWhiteSpace(json));
            }

            return isHealthy;
        }
        catch (RateLimitExceededException ex)
        {
            // Rate limit is actually a sign the API is working, just at capacity
            _logger.LogWarning(ex, "Alpha Vantage health check: Rate limit exceeded (API is functional but at capacity)");
            return true; // Consider healthy since API is responding
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Alpha Vantage health check failed: Network error");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Alpha Vantage health check failed: Request timeout");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alpha Vantage health check failed: Unexpected error");
            return false;
        }
    }

    /// <summary>
    /// Validates the response structure and content
    /// </summary>
    private void ValidateResponseStructure(string json, string endpoint, string symbol)
    {
        // Check if response is empty or whitespace
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogError(
                "Empty response received from Alpha Vantage: Endpoint={Endpoint}, Symbol={Symbol}",
                endpoint,
                symbol);
            throw new ApiUnavailableException(
                $"Empty response received from Alpha Vantage for {symbol}",
                symbol,
                new InvalidOperationException("Response body is empty"));
        }

        // Check if response is valid JSON
        try
        {
            using var doc = JsonDocument.Parse(json);
            
            // Check for common error structures in Alpha Vantage responses
            if (doc.RootElement.TryGetProperty("Error Message", out var errorMessage))
            {
                var error = errorMessage.GetString();
                _logger.LogError(
                    "Alpha Vantage returned error message: Endpoint={Endpoint}, Symbol={Symbol}, Error={Error}",
                    endpoint,
                    symbol,
                    error);
                throw new ApiUnavailableException(
                    $"Alpha Vantage error for {symbol}: {error}",
                    symbol,
                    new InvalidOperationException(error ?? "Unknown error"));
            }

            // Check for information/note messages (often rate limit warnings)
            if (doc.RootElement.TryGetProperty("Information", out var information))
            {
                var info = information.GetString();
                _logger.LogWarning(
                    "Alpha Vantage returned information message: Endpoint={Endpoint}, Symbol={Symbol}, Information={Information}",
                    endpoint,
                    symbol,
                    info);
            }

            if (doc.RootElement.TryGetProperty("Note", out var note))
            {
                var noteText = note.GetString();
                _logger.LogWarning(
                    "Alpha Vantage returned note: Endpoint={Endpoint}, Symbol={Symbol}, Note={Note}",
                    endpoint,
                    symbol,
                    noteText);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Malformed JSON response from Alpha Vantage: Endpoint={Endpoint}, Symbol={Symbol}, ResponseLength={Length}, ResponsePreview={Preview}",
                endpoint,
                symbol,
                json.Length,
                json.Length > 500 ? json.Substring(0, 500) + "..." : json);
            
            throw new ApiUnavailableException(
                $"Malformed JSON response from Alpha Vantage for {symbol}",
                symbol,
                ex);
        }
    }

    /// <summary>
    /// Handles error responses from Alpha Vantage API
    /// </summary>
    private Task HandleErrorResponseAsync(HttpResponseMessage response, string symbol, string content, CancellationToken cancellationToken)
    {
        // Check for authentication errors (invalid API key)
        if (content.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Authentication failed: Invalid Alpha Vantage API key. Symbol={Symbol}, StatusCode={StatusCode}, Response={Response}",
                symbol,
                response.StatusCode,
                content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            
            throw new InvalidApiKeyException(
                "Invalid Alpha Vantage API key. Please check your configuration and ensure the API key is valid.",
                "AlphaVantage");
        }

        // Check for invalid API call errors
        if (content.Contains("Invalid API call", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Invalid API call to Alpha Vantage: Symbol={Symbol}, StatusCode={StatusCode}, Response={Response}",
                symbol,
                response.StatusCode,
                content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            
            throw new ApiUnavailableException(
                $"Invalid API call to Alpha Vantage for {symbol}. The request format may be incorrect.",
                symbol,
                new HttpRequestException($"Status code: {response.StatusCode}"));
        }

        // Check for rate limit errors
        if (content.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("frequency", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Note", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Rate limit exceeded: Symbol={Symbol}, StatusCode={StatusCode}, Response={Response}",
                symbol,
                response.StatusCode,
                content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            
            throw new RateLimitExceededException($"Alpha Vantage rate limit exceeded for {symbol}", symbol);
        }

        // Check for symbol not found
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            content.Contains("Error Message", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Symbol not found: Symbol={Symbol}, StatusCode={StatusCode}",
                symbol,
                response.StatusCode);
            
            throw new SymbolNotFoundException(symbol);
        }

        // Generic API error
        _logger.LogError(
            "Alpha Vantage API error: Symbol={Symbol}, StatusCode={StatusCode}, Response={Response}",
            symbol,
            response.StatusCode,
            content.Length > 200 ? content.Substring(0, 200) + "..." : content);
        
        throw new ApiUnavailableException(
            $"Alpha Vantage API returned status code {response.StatusCode} for {symbol}",
            symbol,
            new HttpRequestException($"Status code: {response.StatusCode}"));
    }

    /// <summary>
    /// Maps Alpha Vantage Global Quote response to MarketData entity
    /// </summary>
    private MarketData MapToMarketData(AlphaVantageGlobalQuote quote, string symbol)
    {
        var currentPrice = ParseDecimal(quote.Price);
        var previousClose = ParseDecimal(quote.PreviousClose);
        var change = ParseDecimal(quote.Change);
        var changePercent = ParseDecimal(quote.ChangePercent?.TrimEnd('%'));

        return new MarketData
        {
            Symbol = symbol,
            CurrentPrice = currentPrice,
            Change = change,
            ChangePercent = changePercent,
            Volume = ParseLong(quote.Volume),
            DayHigh = ParseDecimal(quote.High),
            DayLow = ParseDecimal(quote.Low),
            Timestamp = ParseDateTime(quote.LatestTradingDay) ?? DateTime.UtcNow,
            Exchange = string.Empty, // Alpha Vantage doesn't provide exchange in Global Quote
            MarketState = DetermineMarketState(quote.LatestTradingDay),
            // Fields not provided by Alpha Vantage Global Quote endpoint
            BidPrice = null,
            AskPrice = null,
            FiftyTwoWeekHigh = null,
            FiftyTwoWeekLow = null,
            AverageVolume = null,
            MarketCap = null
        };
    }

    /// <summary>
    /// Determines market state based on latest trading day
    /// </summary>
    private MarketState DetermineMarketState(string? latestTradingDay)
    {
        if (string.IsNullOrEmpty(latestTradingDay))
        {
            return MarketState.Closed;
        }

        var tradingDate = ParseDateTime(latestTradingDay);
        if (!tradingDate.HasValue)
        {
            return MarketState.Closed;
        }

        var now = DateTime.UtcNow;
        var tradingDateUtc = DateTime.SpecifyKind(tradingDate.Value, DateTimeKind.Utc);

        // If trading day is not today, market is closed
        if (tradingDateUtc.Date < now.Date)
        {
            return MarketState.Closed;
        }

        // Simple time-based market state determination (US Eastern Time approximation)
        // This is a simplified version - real implementation would need timezone handling
        var hour = now.Hour;
        
        if (hour >= 14 && hour < 21) // 9:30 AM - 4:00 PM ET (approximate UTC conversion)
        {
            return MarketState.Open;
        }
        else if (hour >= 9 && hour < 14) // Pre-market
        {
            return MarketState.PreMarket;
        }
        else if (hour >= 21 || hour < 1) // After-hours
        {
            return MarketState.AfterHours;
        }

        return MarketState.Closed;
    }

    /// <summary>
    /// Safely parses a string to decimal
    /// </summary>
    private decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0m;
        }

        if (decimal.TryParse(value, out var result))
        {
            return result;
        }

        return 0m;
    }

    /// <summary>
    /// Safely parses a string to long
    /// </summary>
    private long ParseLong(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0L;
        }

        if (long.TryParse(value, out var result))
        {
            return result;
        }

        return 0L;
    }

    /// <summary>
    /// Safely parses a string to DateTime
    /// </summary>
    private DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Maps Alpha Vantage time series data to StockPrice entity
    /// </summary>
    private StockPrice MapToStockPrice(string dateString, AlphaVantageTimeSeriesData data, string symbol)
    {
        var date = ParseDateTime(dateString) ?? DateTime.UtcNow;

        return new StockPrice
        {
            Symbol = symbol,
            Date = date,
            Open = ParseDecimal(data.Open),
            High = ParseDecimal(data.High),
            Low = ParseDecimal(data.Low),
            Close = ParseDecimal(data.Close),
            Volume = ParseLong(data.Volume),
            AdjustedClose = ParseDecimal(data.AdjustedClose ?? data.Close) // Use close if adjusted close not available
        };
    }

    /// <summary>
    /// Maps Alpha Vantage Company Overview response to FundamentalData entity
    /// </summary>
    private FundamentalData MapToFundamentalData(AlphaVantageCompanyOverviewResponse overview, string symbol)
    {
        return new FundamentalData
        {
            Symbol = symbol,
            // Valuation Ratios
            PERatio = ParseNullableDecimal(overview.PERatio),
            PEGRatio = ParseNullableDecimal(overview.PEGRatio),
            PriceToBook = ParseNullableDecimal(overview.PriceToBookRatio),
            PriceToSales = ParseNullableDecimal(overview.PriceToSalesRatioTTM),
            EnterpriseValue = ParseNullableDecimal(overview.EVToRevenue) != null 
                ? ParseNullableDecimal(overview.EVToRevenue) * ParseNullableDecimal(overview.RevenueTTM) 
                : null,
            EVToEBITDA = ParseNullableDecimal(overview.EVToEBITDA),
            // Profitability Metrics
            ProfitMargin = ParseNullableDecimal(overview.ProfitMargin),
            OperatingMargin = ParseNullableDecimal(overview.OperatingMarginTTM),
            ReturnOnEquity = ParseNullableDecimal(overview.ReturnOnEquityTTM),
            ReturnOnAssets = ParseNullableDecimal(overview.ReturnOnAssetsTTM),
            // Growth Metrics
            RevenueGrowth = ParseNullableDecimal(overview.QuarterlyRevenueGrowthYOY),
            EarningsGrowth = ParseNullableDecimal(overview.QuarterlyEarningsGrowthYOY),
            EPS = ParseNullableDecimal(overview.EPS),
            // Dividend Information
            DividendYield = ParseNullableDecimal(overview.DividendYield),
            PayoutRatio = ParseNullableDecimal(overview.PayoutRatio),
            // Financial Health
            CurrentRatio = ParseNullableDecimal(overview.CurrentRatio),
            DebtToEquity = ParseNullableDecimal(overview.DebtToEquity),
            QuickRatio = ParseNullableDecimal(overview.QuickRatio),
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps Alpha Vantage Company Overview response to CompanyProfile entity
    /// </summary>
    private CompanyProfile MapToCompanyProfile(AlphaVantageCompanyOverviewResponse overview, string symbol)
    {
        return new CompanyProfile
        {
            Symbol = symbol,
            CompanyName = overview.Name ?? string.Empty,
            Sector = overview.Sector ?? string.Empty,
            Industry = overview.Industry ?? string.Empty,
            Description = overview.Description ?? string.Empty,
            Website = string.Empty, // Alpha Vantage doesn't provide website in Overview endpoint
            Country = overview.Country ?? string.Empty,
            City = string.Empty, // Alpha Vantage doesn't provide city in Overview endpoint
            EmployeeCount = null, // Alpha Vantage doesn't provide employee count in Overview endpoint
            CEO = string.Empty, // Alpha Vantage doesn't provide CEO in Overview endpoint
            FoundedYear = null, // Alpha Vantage doesn't provide founded year in Overview endpoint
            Exchange = overview.Exchange ?? string.Empty,
            Currency = overview.Currency ?? string.Empty
        };
    }

    /// <summary>
    /// Safely parses a string to nullable decimal
    /// </summary>
    private decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value) || value == "None" || value == "-")
        {
            return null;
        }

        if (decimal.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Maps Alpha Vantage Symbol Search match to StockSearchResult entity
    /// </summary>
    private StockSearchResult MapToStockSearchResult(AlphaVantageSymbolSearchMatch match)
    {
        return new StockSearchResult
        {
            Symbol = match.Symbol ?? string.Empty,
            Name = match.Name ?? string.Empty,
            Exchange = match.Region ?? string.Empty, // Alpha Vantage uses Region field
            AssetType = match.Type ?? string.Empty,
            Region = match.Region ?? string.Empty,
            MatchScore = ParseNullableDecimal(match.MatchScore)
        };
    }
}

/// <summary>
/// Response model for Alpha Vantage Global Quote endpoint
/// </summary>
internal class AlphaVantageGlobalQuoteResponse
{
    [JsonPropertyName("Global Quote")]
    public AlphaVantageGlobalQuote? GlobalQuote { get; set; }
}

/// <summary>
/// Global Quote data from Alpha Vantage
/// </summary>
internal class AlphaVantageGlobalQuote
{
    [JsonPropertyName("01. symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("02. open")]
    public string? Open { get; set; }

    [JsonPropertyName("03. high")]
    public string? High { get; set; }

    [JsonPropertyName("04. low")]
    public string? Low { get; set; }

    [JsonPropertyName("05. price")]
    public string? Price { get; set; }

    [JsonPropertyName("06. volume")]
    public string? Volume { get; set; }

    [JsonPropertyName("07. latest trading day")]
    public string? LatestTradingDay { get; set; }

    [JsonPropertyName("08. previous close")]
    public string? PreviousClose { get; set; }

    [JsonPropertyName("09. change")]
    public string? Change { get; set; }

    [JsonPropertyName("10. change percent")]
    public string? ChangePercent { get; set; }
}

/// <summary>
/// Response model for Alpha Vantage Time Series endpoints
/// </summary>
internal class AlphaVantageTimeSeriesResponse
{
    [JsonPropertyName("Time Series (Daily)")]
    public Dictionary<string, AlphaVantageTimeSeriesData>? TimeSeriesDaily { get; set; }

    [JsonPropertyName("Weekly Time Series")]
    public Dictionary<string, AlphaVantageTimeSeriesData>? TimeSeriesWeekly { get; set; }

    [JsonPropertyName("Monthly Time Series")]
    public Dictionary<string, AlphaVantageTimeSeriesData>? TimeSeriesMonthly { get; set; }

    /// <summary>
    /// Gets the appropriate time series based on which one is populated
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, AlphaVantageTimeSeriesData>? TimeSeries =>
        TimeSeriesDaily ?? TimeSeriesWeekly ?? TimeSeriesMonthly;
}

/// <summary>
/// Time series data point from Alpha Vantage
/// </summary>
internal class AlphaVantageTimeSeriesData
{
    [JsonPropertyName("1. open")]
    public string? Open { get; set; }

    [JsonPropertyName("2. high")]
    public string? High { get; set; }

    [JsonPropertyName("3. low")]
    public string? Low { get; set; }

    [JsonPropertyName("4. close")]
    public string? Close { get; set; }

    [JsonPropertyName("5. volume")]
    public string? Volume { get; set; }

    [JsonPropertyName("6. adjusted close")]
    public string? AdjustedClose { get; set; }
}

/// <summary>
/// Response model for Alpha Vantage Company Overview endpoint
/// </summary>
internal class AlphaVantageCompanyOverviewResponse
{
    [JsonPropertyName("Symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("AssetType")]
    public string? AssetType { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("Exchange")]
    public string? Exchange { get; set; }

    [JsonPropertyName("Currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("Country")]
    public string? Country { get; set; }

    [JsonPropertyName("Sector")]
    public string? Sector { get; set; }

    [JsonPropertyName("Industry")]
    public string? Industry { get; set; }

    // Valuation Ratios
    [JsonPropertyName("PERatio")]
    public string? PERatio { get; set; }

    [JsonPropertyName("PEGRatio")]
    public string? PEGRatio { get; set; }

    [JsonPropertyName("PriceToBookRatio")]
    public string? PriceToBookRatio { get; set; }

    [JsonPropertyName("PriceToSalesRatioTTM")]
    public string? PriceToSalesRatioTTM { get; set; }

    [JsonPropertyName("EVToRevenue")]
    public string? EVToRevenue { get; set; }

    [JsonPropertyName("EVToEBITDA")]
    public string? EVToEBITDA { get; set; }

    // Profitability Metrics
    [JsonPropertyName("ProfitMargin")]
    public string? ProfitMargin { get; set; }

    [JsonPropertyName("OperatingMarginTTM")]
    public string? OperatingMarginTTM { get; set; }

    [JsonPropertyName("ReturnOnEquityTTM")]
    public string? ReturnOnEquityTTM { get; set; }

    [JsonPropertyName("ReturnOnAssetsTTM")]
    public string? ReturnOnAssetsTTM { get; set; }

    // Growth Metrics
    [JsonPropertyName("QuarterlyRevenueGrowthYOY")]
    public string? QuarterlyRevenueGrowthYOY { get; set; }

    [JsonPropertyName("QuarterlyEarningsGrowthYOY")]
    public string? QuarterlyEarningsGrowthYOY { get; set; }

    [JsonPropertyName("EPS")]
    public string? EPS { get; set; }

    [JsonPropertyName("RevenueTTM")]
    public string? RevenueTTM { get; set; }

    // Dividend Information
    [JsonPropertyName("DividendYield")]
    public string? DividendYield { get; set; }

    [JsonPropertyName("PayoutRatio")]
    public string? PayoutRatio { get; set; }

    [JsonPropertyName("DividendPerShare")]
    public string? DividendPerShare { get; set; }

    [JsonPropertyName("DividendDate")]
    public string? DividendDate { get; set; }

    [JsonPropertyName("ExDividendDate")]
    public string? ExDividendDate { get; set; }

    // Financial Health
    [JsonPropertyName("CurrentRatio")]
    public string? CurrentRatio { get; set; }

    [JsonPropertyName("DebtToEquity")]
    public string? DebtToEquity { get; set; }

    [JsonPropertyName("QuickRatio")]
    public string? QuickRatio { get; set; }

    // Market Data
    [JsonPropertyName("MarketCapitalization")]
    public string? MarketCapitalization { get; set; }

    [JsonPropertyName("BookValue")]
    public string? BookValue { get; set; }

    [JsonPropertyName("SharesOutstanding")]
    public string? SharesOutstanding { get; set; }

    [JsonPropertyName("52WeekHigh")]
    public string? FiftyTwoWeekHigh { get; set; }

    [JsonPropertyName("52WeekLow")]
    public string? FiftyTwoWeekLow { get; set; }

    [JsonPropertyName("50DayMovingAverage")]
    public string? FiftyDayMovingAverage { get; set; }

    [JsonPropertyName("200DayMovingAverage")]
    public string? TwoHundredDayMovingAverage { get; set; }
}

/// <summary>
/// Response model for Alpha Vantage Symbol Search endpoint
/// </summary>
internal class AlphaVantageSymbolSearchResponse
{
    [JsonPropertyName("bestMatches")]
    public List<AlphaVantageSymbolSearchMatch>? BestMatches { get; set; }
}

/// <summary>
/// Symbol search match from Alpha Vantage
/// </summary>
internal class AlphaVantageSymbolSearchMatch
{
    [JsonPropertyName("1. symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("2. name")]
    public string? Name { get; set; }

    [JsonPropertyName("3. type")]
    public string? Type { get; set; }

    [JsonPropertyName("4. region")]
    public string? Region { get; set; }

    [JsonPropertyName("5. marketOpen")]
    public string? MarketOpen { get; set; }

    [JsonPropertyName("6. marketClose")]
    public string? MarketClose { get; set; }

    [JsonPropertyName("7. timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("8. currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("9. matchScore")]
    public string? MatchScore { get; set; }
}
