using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;
using StockSensePro.Core.Exceptions;

namespace StockSensePro.Infrastructure.Services
{
    public interface IYahooFinanceService : IStockDataProvider
    {
        Task<Stock?> GetStockAsync(string symbol, CancellationToken cancellationToken = default);
        Task<List<StockPrice>> GetHistoricalPricesAsync(string symbol, int days = 30, CancellationToken cancellationToken = default);
        Task<CompanyInfo?> GetCompanyInfoAsync(string symbol, CancellationToken cancellationToken = default);
    }

    public class YahooFinanceService : IYahooFinanceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<YahooFinanceService> _logger;

        public YahooFinanceService(IHttpClientFactory httpClientFactory, ILogger<YahooFinanceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Helper method to log API requests with timing
        /// </summary>
        private async Task<HttpResponseMessage> ExecuteRequestWithLoggingAsync(
            HttpClient httpClient,
            string endpoint,
            string symbol,
            Func<Task<HttpResponseMessage>> requestFunc,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "API Request: Endpoint={Endpoint}, Symbol={Symbol}, Timestamp={Timestamp}, BaseUrl={BaseUrl}",
                    endpoint,
                    symbol,
                    startTime,
                    httpClient.BaseAddress);

                var response = await requestFunc();
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint={Endpoint}, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms, Timestamp={Timestamp}",
                    endpoint,
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    DateTime.UtcNow);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint={Endpoint}, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, Timestamp={Timestamp}",
                    endpoint,
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    DateTime.UtcNow);
                throw;
            }
        }

        private HttpClient GetChartClient() => _httpClientFactory.CreateClient("YahooFinanceChart");
        private HttpClient GetQuoteClient() => _httpClientFactory.CreateClient("YahooFinanceQuote");
        private HttpClient GetSummaryClient() => _httpClientFactory.CreateClient("YahooFinanceSummary");
        private HttpClient GetSearchClient() => _httpClientFactory.CreateClient("YahooFinanceSearch");

        public async Task<Stock?> GetStockAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "API Request: Endpoint=GetStock, Symbol={Symbol}, Timestamp={Timestamp}",
                    symbol,
                    startTime);

                var httpClient = GetChartClient();
                var response = await httpClient.GetAsync($"{symbol}", cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=GetStock, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooFinanceResponse>(json);

                if (result?.Chart?.Result?.Count > 0)
                {
                    var quote = result.Chart.Result[0];
                    var meta = quote.Meta;
                    var indicators = quote.Indicators;
                    var quotes = indicators.Quote[0];

                    // Get the latest quote data
                    var latestIndex = quotes.Close.Count - 1;
                    if (latestIndex >= 0)
                    {
                        return new Stock
                        {
                            Symbol = meta.Symbol,
                            Name = meta.LongName ?? meta.ShortName ?? meta.Symbol,
                            Exchange = meta.ExchangeName,
                            Sector = meta.Sector ?? "N/A",
                            Industry = meta.Industry ?? "N/A",
                            CurrentPrice = (decimal)(quotes.Close[latestIndex] ?? 0),
                            PreviousClose = (decimal)(meta.PreviousClose ?? 0),
                            Open = (decimal)(quotes.Open[latestIndex] ?? 0),
                            High = (decimal)(quotes.High[latestIndex] ?? 0),
                            Low = (decimal)(quotes.Low[latestIndex] ?? 0),
                            Volume = (long)(quotes.Volume[latestIndex] ?? 0),
                            LastUpdated = DateTime.UtcNow
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetStock, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                return null;
            }
        }

        public Task<List<StockPrice>> GetHistoricalPricesAsync(string symbol, int days = 30, CancellationToken cancellationToken = default)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);
            return GetHistoricalPricesAsync(symbol, startDate, endDate, cancellationToken);
        }

        public async Task<List<StockPrice>> GetHistoricalPricesAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Validate that startDate is before endDate
                if (startDate >= endDate)
                {
                    var message = $"Start date ({startDate:yyyy-MM-dd}) must be earlier than end date ({endDate:yyyy-MM-dd})";
                    _logger.LogWarning(
                        "Invalid date range for {Symbol}: StartDate={StartDate}, EndDate={EndDate}",
                        symbol,
                        startDate,
                        endDate);
                    throw new InvalidDateRangeException(message, symbol);
                }

                // Validate that date range doesn't exceed 5 years
                var maxDateRange = TimeSpan.FromDays(365 * 5); // 5 years
                var requestedRange = endDate - startDate;
                if (requestedRange > maxDateRange)
                {
                    var message = $"Date range exceeds maximum allowed period of 5 years. Requested: {requestedRange.Days} days ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})";
                    _logger.LogWarning(
                        "Date range too large for {Symbol}: StartDate={StartDate}, EndDate={EndDate}, Days={Days}",
                        symbol,
                        startDate,
                        endDate,
                        requestedRange.Days);
                    throw new InvalidDateRangeException(message, symbol);
                }

                _logger.LogInformation(
                    "API Request: Endpoint=GetHistoricalPrices, Symbol={Symbol}, StartDate={StartDate}, EndDate={EndDate}, Timestamp={Timestamp}",
                    symbol,
                    startDate,
                    endDate,
                    startTime);

                var httpClient = GetChartClient();
                var response = await httpClient.GetAsync(
                    $"{symbol}?period1={ToUnixTimestamp(startDate)}&period2={ToUnixTimestamp(endDate)}&interval=1d",
                    cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=GetHistoricalPrices, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooFinanceResponse>(json);

                var prices = new List<StockPrice>();

                if (result?.Chart?.Result?.Count > 0)
                {
                    var quote = result.Chart.Result[0];
                    var timestamps = quote.Timestamp;
                    var indicators = quote.Indicators;
                    var quotes = indicators.Quote[0];

                    for (int i = 0; i < timestamps.Count; i++)
                    {
                        if (i < quotes.Close.Count &&
                            quotes.Open.Count > i &&
                            quotes.High.Count > i &&
                            quotes.Low.Count > i &&
                            quotes.Volume.Count > i)
                        {
                            prices.Add(new StockPrice
                            {
                                Symbol = symbol,
                                Date = FromUnixTimestamp(timestamps[i]),
                                Open = (decimal)(quotes.Open[i] ?? 0),
                                High = (decimal)(quotes.High[i] ?? 0),
                                Low = (decimal)(quotes.Low[i] ?? 0),
                                Close = (decimal)(quotes.Close[i] ?? 0),
                                Volume = (long)(quotes.Volume[i] ?? 0),
                                AdjustedClose = (decimal)(quotes.Close[i] ?? 0) // For simplicity, using close as adjusted close
                            });
                        }
                    }
                }

                _logger.LogInformation(
                    "Successfully fetched {Count} historical prices for {Symbol}",
                    prices.Count,
                    symbol);

                return prices;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetHistoricalPrices, Symbol={Symbol}, StartDate={StartDate}, EndDate={EndDate}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    symbol,
                    startDate,
                    endDate,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                return new List<StockPrice>();
            }
        }

        public async Task<CompanyInfo?> GetCompanyInfoAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would fetch company info from Yahoo Finance
            // For now, we'll return null to indicate this feature needs implementation
            return await Task.FromResult<CompanyInfo?>(null);
        }

        // IStockDataProvider implementation
        public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "API Request: Endpoint=GetQuote, Symbol={Symbol}, Timestamp={Timestamp}",
                    symbol,
                    startTime);

                // Use the quote endpoint which provides comprehensive market data
                var httpClient = GetQuoteClient();
                var response = await httpClient.GetAsync($"?symbols={symbol}", cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=GetQuote, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning(
                            "API Error: Endpoint=GetQuote, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms, Error=SymbolNotFound",
                            symbol,
                            response.StatusCode,
                            stopwatch.ElapsedMilliseconds);
                        throw new SymbolNotFoundException(symbol);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning(
                            "API Error: Endpoint=GetQuote, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms, Error=RateLimitExceeded",
                            symbol,
                            response.StatusCode,
                            stopwatch.ElapsedMilliseconds);
                        throw new RateLimitExceededException($"Rate limit exceeded while fetching quote for {symbol}", symbol);
                    }
                    else
                    {
                        _logger.LogError(
                            "API Error: Endpoint=GetQuote, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms, Error=ApiUnavailable",
                            symbol,
                            response.StatusCode,
                            stopwatch.ElapsedMilliseconds);
                        throw new ApiUnavailableException($"Yahoo Finance API returned status code {response.StatusCode}", symbol, 
                            new HttpRequestException($"Status code: {response.StatusCode}"));
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooQuoteResponse>(json);

                if (result?.QuoteResponse?.Result == null || result.QuoteResponse.Result.Count == 0)
                {
                    _logger.LogWarning("No data available for symbol {Symbol}", symbol);
                    throw new SymbolNotFoundException(symbol);
                }

                var quoteData = result.QuoteResponse.Result[0];

                // Validate that we have essential data
                if (!quoteData.RegularMarketPrice.HasValue)
                {
                    _logger.LogWarning("Invalid quote data for symbol {Symbol} - missing price", symbol);
                    throw new SymbolNotFoundException(symbol);
                }

                var currentPrice = (decimal)quoteData.RegularMarketPrice.Value;
                var previousClose = (decimal)(quoteData.RegularMarketPreviousClose ?? quoteData.RegularMarketPrice.Value);
                var change = currentPrice - previousClose;
                var changePercent = previousClose != 0 ? (change / previousClose) * 100 : 0;

                // Determine market state
                var marketState = DetermineMarketState(quoteData.MarketState);

                var marketData = new MarketData
                {
                    Symbol = quoteData.Symbol,
                    CurrentPrice = currentPrice,
                    Change = change,
                    ChangePercent = changePercent,
                    Volume = quoteData.RegularMarketVolume ?? 0,
                    BidPrice = quoteData.Bid.HasValue ? (decimal)quoteData.Bid.Value : null,
                    AskPrice = quoteData.Ask.HasValue ? (decimal)quoteData.Ask.Value : null,
                    DayHigh = quoteData.RegularMarketDayHigh.HasValue ? (decimal)quoteData.RegularMarketDayHigh.Value : currentPrice,
                    DayLow = quoteData.RegularMarketDayLow.HasValue ? (decimal)quoteData.RegularMarketDayLow.Value : currentPrice,
                    FiftyTwoWeekHigh = quoteData.FiftyTwoWeekHigh.HasValue ? (decimal)quoteData.FiftyTwoWeekHigh.Value : null,
                    FiftyTwoWeekLow = quoteData.FiftyTwoWeekLow.HasValue ? (decimal)quoteData.FiftyTwoWeekLow.Value : null,
                    AverageVolume = quoteData.AverageDailyVolume3Month,
                    MarketCap = quoteData.MarketCap,
                    Timestamp = DateTime.UtcNow,
                    Exchange = quoteData.FullExchangeName ?? quoteData.Exchange ?? string.Empty,
                    MarketState = marketState
                };

                _logger.LogInformation(
                    "API Success: Endpoint=GetQuote, Symbol={Symbol}, Price={Price}, Change={ChangePercent}%, Volume={Volume}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    marketData.CurrentPrice,
                    marketData.ChangePercent,
                    marketData.Volume,
                    stopwatch.ElapsedMilliseconds);

                return marketData;
            }
            catch (SymbolNotFoundException)
            {
                throw;
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (ApiUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuote, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=NetworkError, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Network error while fetching quote for {symbol}", symbol, ex);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuote, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=Timeout, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Request timeout while fetching quote for {symbol}", symbol, ex);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuote, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=ParseError, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Failed to parse Yahoo Finance response for {symbol}", symbol, ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuote, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw new ApiUnavailableException($"Unexpected error while fetching quote for {symbol}", symbol, ex);
            }
        }

        private MarketState DetermineMarketState(string? marketStateStr)
        {
            if (string.IsNullOrEmpty(marketStateStr))
            {
                return MarketState.Closed;
            }

            return marketStateStr.ToUpperInvariant() switch
            {
                "REGULAR" => MarketState.Open,
                "PRE" => MarketState.PreMarket,
                "POST" => MarketState.AfterHours,
                "CLOSED" => MarketState.Closed,
                _ => MarketState.Closed
            };
        }

        public async Task<List<MarketData>> GetQuotesAsync(List<string> symbols, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (symbols == null || symbols.Count == 0)
            {
                _logger.LogWarning("GetQuotesAsync called with empty symbol list");
                return new List<MarketData>();
            }

            try
            {
                _logger.LogInformation(
                    "API Request: Endpoint=GetQuotes, SymbolCount={Count}, Symbols={Symbols}, Timestamp={Timestamp}",
                    symbols.Count,
                    string.Join(",", symbols),
                    startTime);

                // Yahoo Finance supports batch requests with comma-separated symbols
                var symbolsParam = string.Join(",", symbols);
                var httpClient = GetQuoteClient();
                var response = await httpClient.GetAsync($"?symbols={symbolsParam}", cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=GetQuotes, SymbolCount={Count}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    symbols.Count,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limit exceeded for batch quote request");
                        throw new RateLimitExceededException("Rate limit exceeded while fetching batch quotes");
                    }
                    else
                    {
                        _logger.LogError("API returned status code {StatusCode} for batch quote request", response.StatusCode);
                        throw new ApiUnavailableException($"Yahoo Finance API returned status code {response.StatusCode}", 
                            new HttpRequestException($"Status code: {response.StatusCode}"));
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooQuoteResponse>(json);

                if (result?.QuoteResponse?.Result == null || result.QuoteResponse.Result.Count == 0)
                {
                    _logger.LogWarning("No data available for batch quote request");
                    return new List<MarketData>();
                }

                var quotes = new List<MarketData>();

                foreach (var quoteData in result.QuoteResponse.Result)
                {
                    try
                    {
                        // Skip quotes without essential data
                        if (!quoteData.RegularMarketPrice.HasValue)
                        {
                            _logger.LogWarning("Skipping symbol {Symbol} - missing price data", quoteData.Symbol);
                            continue;
                        }

                        var currentPrice = (decimal)quoteData.RegularMarketPrice.Value;
                        var previousClose = (decimal)(quoteData.RegularMarketPreviousClose ?? quoteData.RegularMarketPrice.Value);
                        var change = currentPrice - previousClose;
                        var changePercent = previousClose != 0 ? (change / previousClose) * 100 : 0;

                        var marketState = DetermineMarketState(quoteData.MarketState);

                        var marketData = new MarketData
                        {
                            Symbol = quoteData.Symbol,
                            CurrentPrice = currentPrice,
                            Change = change,
                            ChangePercent = changePercent,
                            Volume = quoteData.RegularMarketVolume ?? 0,
                            BidPrice = quoteData.Bid.HasValue ? (decimal)quoteData.Bid.Value : null,
                            AskPrice = quoteData.Ask.HasValue ? (decimal)quoteData.Ask.Value : null,
                            DayHigh = quoteData.RegularMarketDayHigh.HasValue ? (decimal)quoteData.RegularMarketDayHigh.Value : currentPrice,
                            DayLow = quoteData.RegularMarketDayLow.HasValue ? (decimal)quoteData.RegularMarketDayLow.Value : currentPrice,
                            FiftyTwoWeekHigh = quoteData.FiftyTwoWeekHigh.HasValue ? (decimal)quoteData.FiftyTwoWeekHigh.Value : null,
                            FiftyTwoWeekLow = quoteData.FiftyTwoWeekLow.HasValue ? (decimal)quoteData.FiftyTwoWeekLow.Value : null,
                            AverageVolume = quoteData.AverageDailyVolume3Month,
                            MarketCap = quoteData.MarketCap,
                            Timestamp = DateTime.UtcNow,
                            Exchange = quoteData.FullExchangeName ?? quoteData.Exchange ?? string.Empty,
                            MarketState = marketState
                        };

                        quotes.Add(marketData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process quote data for symbol {Symbol}", quoteData.Symbol);
                        // Continue with other symbols
                    }
                }

                _logger.LogInformation(
                    "API Success: Endpoint=GetQuotes, SuccessCount={SuccessCount}, RequestedCount={RequestedCount}, ResponseTime={ResponseTimeMs}ms",
                    quotes.Count,
                    symbols.Count,
                    stopwatch.ElapsedMilliseconds);

                return quotes;
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (ApiUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuotes, SymbolCount={Count}, ResponseTime={ResponseTimeMs}ms, ErrorType=NetworkError, ErrorMessage={ErrorMessage}",
                    symbols.Count,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException("Network error while fetching batch quotes", ex);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuotes, SymbolCount={Count}, ResponseTime={ResponseTimeMs}ms, ErrorType=Timeout, ErrorMessage={ErrorMessage}",
                    symbols.Count,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException("Request timeout while fetching batch quotes", ex);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuotes, SymbolCount={Count}, ResponseTime={ResponseTimeMs}ms, ErrorType=ParseError, ErrorMessage={ErrorMessage}",
                    symbols.Count,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException("Failed to parse Yahoo Finance batch quote response", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetQuotes, SymbolCount={Count}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    symbols.Count,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw new ApiUnavailableException("Unexpected error while fetching batch quotes", ex);
            }
        }

        public async Task<List<StockPrice>> GetHistoricalPricesAsync(
            string symbol,
            DateTime startDate,
            DateTime endDate,
            TimeInterval interval = TimeInterval.Daily,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Validate that startDate is before endDate
                if (startDate >= endDate)
                {
                    var message = $"Start date ({startDate:yyyy-MM-dd}) must be earlier than end date ({endDate:yyyy-MM-dd})";
                    _logger.LogWarning(
                        "Invalid date range for {Symbol}: StartDate={StartDate}, EndDate={EndDate}",
                        symbol,
                        startDate,
                        endDate);
                    throw new InvalidDateRangeException(message, symbol);
                }

                // Validate that date range doesn't exceed 5 years
                var maxDateRange = TimeSpan.FromDays(365 * 5); // 5 years
                var requestedRange = endDate - startDate;
                if (requestedRange > maxDateRange)
                {
                    var message = $"Date range exceeds maximum allowed period of 5 years. Requested: {requestedRange.Days} days ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})";
                    _logger.LogWarning(
                        "Date range too large for {Symbol}: StartDate={StartDate}, EndDate={EndDate}, Days={Days}",
                        symbol,
                        startDate,
                        endDate,
                        requestedRange.Days);
                    throw new InvalidDateRangeException(message, symbol);
                }

                // Map TimeInterval enum to Yahoo Finance interval parameter
                var intervalParam = interval switch
                {
                    TimeInterval.Daily => "1d",
                    TimeInterval.Weekly => "1wk",
                    TimeInterval.Monthly => "1mo",
                    _ => "1d"
                };

                _logger.LogInformation(
                    "API Request: Endpoint=GetHistoricalPrices, Symbol={Symbol}, StartDate={StartDate}, EndDate={EndDate}, Interval={Interval}, Timestamp={Timestamp}",
                    symbol,
                    startDate,
                    endDate,
                    interval,
                    startTime);

                var httpClient = GetChartClient();
                var response = await httpClient.GetAsync(
                    $"{symbol}?period1={ToUnixTimestamp(startDate)}&period2={ToUnixTimestamp(endDate)}&interval={intervalParam}",
                    cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=GetHistoricalPrices, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooFinanceResponse>(json);

                var prices = new List<StockPrice>();

                if (result?.Chart?.Result?.Count > 0)
                {
                    var quote = result.Chart.Result[0];
                    var timestamps = quote.Timestamp;
                    var indicators = quote.Indicators;
                    var quotes = indicators.Quote[0];

                    for (int i = 0; i < timestamps.Count; i++)
                    {
                        if (i < quotes.Close.Count &&
                            quotes.Open.Count > i &&
                            quotes.High.Count > i &&
                            quotes.Low.Count > i &&
                            quotes.Volume.Count > i)
                        {
                            prices.Add(new StockPrice
                            {
                                Symbol = symbol,
                                Date = FromUnixTimestamp(timestamps[i]),
                                Open = (decimal)(quotes.Open[i] ?? 0),
                                High = (decimal)(quotes.High[i] ?? 0),
                                Low = (decimal)(quotes.Low[i] ?? 0),
                                Close = (decimal)(quotes.Close[i] ?? 0),
                                Volume = (long)(quotes.Volume[i] ?? 0),
                                AdjustedClose = (decimal)(quotes.Close[i] ?? 0)
                            });
                        }
                    }
                }

                _logger.LogInformation(
                    "API Success: Endpoint=GetHistoricalPrices, Symbol={Symbol}, PriceCount={Count}, Interval={Interval}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    prices.Count,
                    interval,
                    stopwatch.ElapsedMilliseconds);

                return prices;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetHistoricalPrices, Symbol={Symbol}, StartDate={StartDate}, EndDate={EndDate}, Interval={Interval}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    symbol,
                    startDate,
                    endDate,
                    interval,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw;
            }
        }

        public async Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "API Request: Endpoint=GetFundamentals, Symbol={Symbol}, Timestamp={Timestamp}",
                    symbol,
                    startTime);

                // Yahoo Finance quoteSummary endpoint provides comprehensive fundamental data
                var modules = "defaultKeyStatistics,financialData,summaryDetail";
                var httpClient = GetSummaryClient();
                var response = await httpClient.GetAsync($"{symbol}?modules={modules}", cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=GetFundamentals, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Symbol {Symbol} not found for fundamental data", symbol);
                        throw new SymbolNotFoundException(symbol);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limit exceeded for symbol {Symbol}", symbol);
                        throw new RateLimitExceededException($"Rate limit exceeded while fetching fundamentals for {symbol}", symbol);
                    }
                    else
                    {
                        _logger.LogError("API returned status code {StatusCode} for symbol {Symbol}", response.StatusCode, symbol);
                        throw new ApiUnavailableException($"Yahoo Finance API returned status code {response.StatusCode}", symbol,
                            new HttpRequestException($"Status code: {response.StatusCode}"));
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooFundamentalsResponse>(json);

                if (result?.QuoteSummary?.Result == null || result.QuoteSummary.Result.Count == 0)
                {
                    _logger.LogWarning("No fundamental data available for symbol {Symbol}", symbol);
                    throw new SymbolNotFoundException(symbol);
                }

                var data = result.QuoteSummary.Result[0];
                var keyStats = data.DefaultKeyStatistics;
                var financialData = data.FinancialData;
                var summaryDetail = data.SummaryDetail;

                // Map Yahoo Finance response to FundamentalData model
                var fundamentalData = new FundamentalData
                {
                    Symbol = symbol,
                    
                    // Valuation Ratios
                    PERatio = GetDecimalValue(keyStats?.TrailingPE) ?? GetDecimalValue(keyStats?.ForwardPE),
                    PEGRatio = GetDecimalValue(keyStats?.PegRatio),
                    PriceToBook = GetDecimalValue(keyStats?.PriceToBook),
                    PriceToSales = GetDecimalValue(keyStats?.PriceToSalesTrailing12Months),
                    EnterpriseValue = GetDecimalValue(keyStats?.EnterpriseValue),
                    EVToEBITDA = GetDecimalValue(keyStats?.EnterpriseToEbitda),
                    
                    // Profitability Metrics
                    ProfitMargin = GetDecimalValue(financialData?.ProfitMargins),
                    OperatingMargin = GetDecimalValue(financialData?.OperatingMargins),
                    ReturnOnEquity = GetDecimalValue(financialData?.ReturnOnEquity),
                    ReturnOnAssets = GetDecimalValue(financialData?.ReturnOnAssets),
                    
                    // Growth Metrics
                    RevenueGrowth = GetDecimalValue(financialData?.RevenueGrowth),
                    EarningsGrowth = GetDecimalValue(financialData?.EarningsGrowth),
                    EPS = GetDecimalValue(keyStats?.TrailingEps) ?? GetDecimalValue(keyStats?.ForwardEps),
                    
                    // Dividend Information
                    DividendYield = GetDecimalValue(summaryDetail?.DividendYield),
                    PayoutRatio = GetDecimalValue(summaryDetail?.PayoutRatio),
                    
                    // Financial Health
                    CurrentRatio = GetDecimalValue(financialData?.CurrentRatio),
                    DebtToEquity = GetDecimalValue(financialData?.DebtToEquity),
                    QuickRatio = GetDecimalValue(financialData?.QuickRatio),
                    
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "API Success: Endpoint=GetFundamentals, Symbol={Symbol}, PERatio={PERatio}, EPS={EPS}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    fundamentalData.PERatio,
                    fundamentalData.EPS,
                    stopwatch.ElapsedMilliseconds);

                return fundamentalData;
            }
            catch (SymbolNotFoundException)
            {
                throw;
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (ApiUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetFundamentals, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=NetworkError, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Network error while fetching fundamentals for {symbol}", symbol, ex);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetFundamentals, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=Timeout, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Request timeout while fetching fundamentals for {symbol}", symbol, ex);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetFundamentals, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=ParseError, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Failed to parse Yahoo Finance fundamental data response for {symbol}", symbol, ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetFundamentals, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw new ApiUnavailableException($"Unexpected error while fetching fundamentals for {symbol}", symbol, ex);
            }
        }

        /// <summary>
        /// Helper method to safely extract decimal values from Yahoo Finance raw value objects
        /// </summary>
        private decimal? GetDecimalValue(YahooRawValue? rawValue)
        {
            if (rawValue?.Raw == null)
            {
                return null;
            }

            try
            {
                return (decimal)rawValue.Raw.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "API Request: Endpoint=GetCompanyProfile, Symbol={Symbol}, Timestamp={Timestamp}",
                    symbol,
                    startTime);

                // Yahoo Finance quoteSummary endpoint with assetProfile and summaryProfile modules
                var modules = "assetProfile,summaryProfile,price";
                var httpClient = GetSummaryClient();
                var response = await httpClient.GetAsync($"{symbol}?modules={modules}", cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=GetCompanyProfile, Symbol={Symbol}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Symbol {Symbol} not found for company profile", symbol);
                        throw new SymbolNotFoundException(symbol);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limit exceeded for symbol {Symbol}", symbol);
                        throw new RateLimitExceededException($"Rate limit exceeded while fetching company profile for {symbol}", symbol);
                    }
                    else
                    {
                        _logger.LogError("API returned status code {StatusCode} for symbol {Symbol}", response.StatusCode, symbol);
                        throw new ApiUnavailableException($"Yahoo Finance API returned status code {response.StatusCode}", symbol,
                            new HttpRequestException($"Status code: {response.StatusCode}"));
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooProfileResponse>(json);

                if (result?.QuoteSummary?.Result == null || result.QuoteSummary.Result.Count == 0)
                {
                    _logger.LogWarning("No company profile data available for symbol {Symbol}", symbol);
                    throw new SymbolNotFoundException(symbol);
                }

                var data = result.QuoteSummary.Result[0];
                var assetProfile = data.AssetProfile;
                var price = data.Price;

                // Map Yahoo Finance response to CompanyProfile model
                var companyProfile = new CompanyProfile
                {
                    Symbol = symbol,
                    CompanyName = price?.LongName ?? price?.ShortName ?? symbol,
                    Sector = assetProfile?.Sector ?? string.Empty,
                    Industry = assetProfile?.Industry ?? string.Empty,
                    Description = assetProfile?.LongBusinessSummary ?? string.Empty,
                    Website = assetProfile?.Website ?? string.Empty,
                    Country = assetProfile?.Country ?? string.Empty,
                    City = assetProfile?.City ?? string.Empty,
                    EmployeeCount = assetProfile?.FullTimeEmployees,
                    CEO = GetCEOName(assetProfile?.CompanyOfficers),
                    Exchange = price?.ExchangeName ?? string.Empty,
                    Currency = price?.Currency ?? string.Empty
                };

                _logger.LogInformation(
                    "API Success: Endpoint=GetCompanyProfile, Symbol={Symbol}, CompanyName={CompanyName}, Sector={Sector}, Industry={Industry}, ResponseTime={ResponseTimeMs}ms",
                    symbol,
                    companyProfile.CompanyName,
                    companyProfile.Sector,
                    companyProfile.Industry,
                    stopwatch.ElapsedMilliseconds);

                return companyProfile;
            }
            catch (SymbolNotFoundException)
            {
                throw;
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (ApiUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetCompanyProfile, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=NetworkError, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Network error while fetching company profile for {symbol}", symbol, ex);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetCompanyProfile, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=Timeout, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Request timeout while fetching company profile for {symbol}", symbol, ex);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetCompanyProfile, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType=ParseError, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Failed to parse Yahoo Finance company profile response for {symbol}", symbol, ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=GetCompanyProfile, Symbol={Symbol}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    symbol,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw new ApiUnavailableException($"Unexpected error while fetching company profile for {symbol}", symbol, ex);
            }
        }

        /// <summary>
        /// Helper method to extract CEO name from company officers list
        /// </summary>
        private string GetCEOName(List<CompanyOfficer>? officers)
        {
            if (officers == null || officers.Count == 0)
            {
                return string.Empty;
            }

            // Look for CEO, Chief Executive Officer, or President
            var ceo = officers.FirstOrDefault(o => 
                o.Title?.Contains("CEO", StringComparison.OrdinalIgnoreCase) == true ||
                o.Title?.Contains("Chief Executive Officer", StringComparison.OrdinalIgnoreCase) == true);

            if (ceo != null)
            {
                return ceo.Name ?? string.Empty;
            }

            // Fallback to first officer if no CEO found
            return officers[0].Name ?? string.Empty;
        }

        public async Task<List<StockSearchResult>> SearchSymbolsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    _logger.LogWarning("SearchSymbolsAsync called with empty query");
                    return new List<StockSearchResult>();
                }

                _logger.LogInformation(
                    "API Request: Endpoint=SearchSymbols, Query={Query}, Limit={Limit}, Timestamp={Timestamp}",
                    query,
                    limit,
                    startTime);

                // Yahoo Finance search/autocomplete endpoint
                var httpClient = GetSearchClient();
                var response = await httpClient.GetAsync($"?q={Uri.EscapeDataString(query)}&quotesCount={limit}&newsCount=0", cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "API Response: Endpoint=SearchSymbols, Query={Query}, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms",
                    query,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Rate limit exceeded for search query {Query}", query);
                        throw new RateLimitExceededException($"Rate limit exceeded while searching for {query}");
                    }
                    else
                    {
                        _logger.LogError("API returned status code {StatusCode} for search query {Query}", response.StatusCode, query);
                        throw new ApiUnavailableException($"Yahoo Finance API returned status code {response.StatusCode}", 
                            new HttpRequestException($"Status code: {response.StatusCode}"));
                    }
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YahooSearchResponse>(json);

                if (result?.Quotes == null || result.Quotes.Count == 0)
                {
                    _logger.LogInformation("No search results found for query {Query}", query);
                    return new List<StockSearchResult>();
                }

                var searchResults = new List<StockSearchResult>();

                foreach (var quote in result.Quotes.Take(limit))
                {
                    // Filter to only include stocks and ETFs (exclude indices, currencies, etc.)
                    if (string.IsNullOrEmpty(quote.Symbol))
                    {
                        continue;
                    }

                    // Calculate match score based on query relevance
                    var matchScore = CalculateMatchScore(query, quote.Symbol, quote.ShortName ?? quote.LongName);

                    var searchResult = new StockSearchResult
                    {
                        Symbol = quote.Symbol,
                        Name = quote.LongName ?? quote.ShortName ?? quote.Symbol,
                        Exchange = quote.Exchange ?? string.Empty,
                        AssetType = MapQuoteType(quote.QuoteType),
                        Region = quote.TypeDisp ?? string.Empty,
                        MatchScore = matchScore
                    };

                    searchResults.Add(searchResult);
                }

                // Sort by match score (highest first) and then by symbol
                var sortedResults = searchResults
                    .OrderByDescending(r => r.MatchScore)
                    .ThenBy(r => r.Symbol)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation(
                    "API Success: Endpoint=SearchSymbols, Query={Query}, ResultCount={Count}, ResponseTime={ResponseTimeMs}ms",
                    query,
                    sortedResults.Count,
                    stopwatch.ElapsedMilliseconds);

                return sortedResults;
            }
            catch (RateLimitExceededException)
            {
                throw;
            }
            catch (ApiUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=SearchSymbols, Query={Query}, ResponseTime={ResponseTimeMs}ms, ErrorType=NetworkError, ErrorMessage={ErrorMessage}",
                    query,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Network error while searching for {query}", ex);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=SearchSymbols, Query={Query}, ResponseTime={ResponseTimeMs}ms, ErrorType=Timeout, ErrorMessage={ErrorMessage}",
                    query,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Request timeout while searching for {query}", ex);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=SearchSymbols, Query={Query}, ResponseTime={ResponseTimeMs}ms, ErrorType=ParseError, ErrorMessage={ErrorMessage}",
                    query,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw new ApiUnavailableException($"Failed to parse Yahoo Finance search response for {query}", ex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "API Request Failed: Endpoint=SearchSymbols, Query={Query}, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                    query,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message);
                throw new ApiUnavailableException($"Unexpected error while searching for {query}", ex);
            }
        }

        /// <summary>
        /// Calculates a match score for search results based on query relevance
        /// Higher score means better match
        /// </summary>
        private decimal CalculateMatchScore(string query, string symbol, string? name)
        {
            if (string.IsNullOrEmpty(query))
            {
                return 0;
            }

            var queryLower = query.ToLowerInvariant();
            var symbolLower = symbol.ToLowerInvariant();
            var nameLower = (name ?? string.Empty).ToLowerInvariant();

            decimal score = 0;

            // Exact symbol match gets highest score
            if (symbolLower == queryLower)
            {
                score += 100;
            }
            // Symbol starts with query gets high score
            else if (symbolLower.StartsWith(queryLower))
            {
                score += 80;
            }
            // Symbol contains query gets medium score
            else if (symbolLower.Contains(queryLower))
            {
                score += 50;
            }

            // Exact name match gets high score
            if (nameLower == queryLower)
            {
                score += 90;
            }
            // Name starts with query gets good score
            else if (nameLower.StartsWith(queryLower))
            {
                score += 60;
            }
            // Name contains query as a word gets medium score
            else if (nameLower.Contains(" " + queryLower + " ") || 
                     nameLower.StartsWith(queryLower + " ") || 
                     nameLower.EndsWith(" " + queryLower))
            {
                score += 40;
            }
            // Name contains query anywhere gets lower score
            else if (nameLower.Contains(queryLower))
            {
                score += 20;
            }

            // Boost score for shorter symbols (usually more relevant)
            if (symbol.Length <= 5)
            {
                score += 10;
            }

            return score;
        }

        /// <summary>
        /// Maps Yahoo Finance quote type to standardized asset type
        /// </summary>
        private string MapQuoteType(string? quoteType)
        {
            if (string.IsNullOrEmpty(quoteType))
            {
                return "Unknown";
            }

            return quoteType.ToUpperInvariant() switch
            {
                "EQUITY" => "Stock",
                "ETF" => "ETF",
                "INDEX" => "Index",
                "MUTUALFUND" => "Mutual Fund",
                "CURRENCY" => "Currency",
                "CRYPTOCURRENCY" => "Cryptocurrency",
                "FUTURE" => "Future",
                "OPTION" => "Option",
                _ => quoteType
            };
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "Health Check: Testing Yahoo Finance API connectivity, Timestamp={Timestamp}",
                    startTime);

                // Test connectivity by fetching a well-known symbol (AAPL)
                var httpClient = GetChartClient();
                var response = await httpClient.GetAsync("AAPL", cancellationToken);
                stopwatch.Stop();

                var isHealthy = response.IsSuccessStatusCode;

                if (isHealthy)
                {
                    _logger.LogInformation(
                        "Health Check: Yahoo Finance API is healthy, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms, Timestamp={Timestamp}",
                        response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        DateTime.UtcNow);
                }
                else
                {
                    _logger.LogWarning(
                        "Health Check: Yahoo Finance API returned unhealthy status, StatusCode={StatusCode}, ResponseTime={ResponseTimeMs}ms, Timestamp={Timestamp}",
                        response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        DateTime.UtcNow);
                }

                return isHealthy;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Health Check: Yahoo Finance API health check failed, ResponseTime={ResponseTimeMs}ms, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}, Timestamp={Timestamp}",
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name,
                    ex.Message,
                    DateTime.UtcNow);
                return false;
            }
        }

        private long ToUnixTimestamp(DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }

        private DateTime FromUnixTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }
    }

    // Yahoo Finance Quote API response models
    public class YahooQuoteResponse
    {
        [JsonPropertyName("quoteResponse")]
        public QuoteResponse QuoteResponse { get; set; } = new();
    }

    public class QuoteResponse
    {
        [JsonPropertyName("result")]
        public List<QuoteData> Result { get; set; } = new();

        [JsonPropertyName("error")]
        public object? Error { get; set; }
    }

    public class QuoteData
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("regularMarketPrice")]
        public double? RegularMarketPrice { get; set; }

        [JsonPropertyName("regularMarketChange")]
        public double? RegularMarketChange { get; set; }

        [JsonPropertyName("regularMarketChangePercent")]
        public double? RegularMarketChangePercent { get; set; }

        [JsonPropertyName("regularMarketVolume")]
        public long? RegularMarketVolume { get; set; }

        [JsonPropertyName("regularMarketPreviousClose")]
        public double? RegularMarketPreviousClose { get; set; }

        [JsonPropertyName("regularMarketDayHigh")]
        public double? RegularMarketDayHigh { get; set; }

        [JsonPropertyName("regularMarketDayLow")]
        public double? RegularMarketDayLow { get; set; }

        [JsonPropertyName("bid")]
        public double? Bid { get; set; }

        [JsonPropertyName("ask")]
        public double? Ask { get; set; }

        [JsonPropertyName("fiftyTwoWeekHigh")]
        public double? FiftyTwoWeekHigh { get; set; }

        [JsonPropertyName("fiftyTwoWeekLow")]
        public double? FiftyTwoWeekLow { get; set; }

        [JsonPropertyName("averageDailyVolume3Month")]
        public long? AverageDailyVolume3Month { get; set; }

        [JsonPropertyName("marketCap")]
        public long? MarketCap { get; set; }

        [JsonPropertyName("exchange")]
        public string? Exchange { get; set; }

        [JsonPropertyName("fullExchangeName")]
        public string? FullExchangeName { get; set; }

        [JsonPropertyName("marketState")]
        public string? MarketState { get; set; }
    }

    // Yahoo Finance Chart API response models
    public class YahooFinanceResponse
    {
        [JsonPropertyName("chart")]
        public Chart? Chart { get; set; }
    }

    public class Chart
    {
        [JsonPropertyName("result")]
        public List<ChartResult>? Result { get; set; }
    }

    public class ChartResult
    {
        [JsonPropertyName("meta")]
        public Meta Meta { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public List<long> Timestamp { get; set; } = new();

        [JsonPropertyName("indicators")]
        public Indicators Indicators { get; set; } = new();
    }

    public class Meta
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("longName")]
        public string? LongName { get; set; }

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("exchangeName")]
        public string ExchangeName { get; set; } = string.Empty;

        [JsonPropertyName("sector")]
        public string? Sector { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("previousClose")]
        public double? PreviousClose { get; set; }
    }

    public class Indicators
    {
        [JsonPropertyName("quote")]
        public List<Quote> Quote { get; set; } = new();
    }

    public class Quote
    {
        [JsonPropertyName("open")]
        public List<double?> Open { get; set; } = new();

        [JsonPropertyName("high")]
        public List<double?> High { get; set; } = new();

        [JsonPropertyName("low")]
        public List<double?> Low { get; set; } = new();

        [JsonPropertyName("close")]
        public List<double?> Close { get; set; } = new();

        [JsonPropertyName("volume")]
        public List<long?> Volume { get; set; } = new();
    }

    public class CompanyInfo
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string BusinessSummary { get; set; } = string.Empty;
    }

    // Yahoo Finance Fundamentals API response models
    public class YahooFundamentalsResponse
    {
        [JsonPropertyName("quoteSummary")]
        public QuoteSummary QuoteSummary { get; set; } = new();
    }

    public class QuoteSummary
    {
        [JsonPropertyName("result")]
        public List<QuoteSummaryResult> Result { get; set; } = new();

        [JsonPropertyName("error")]
        public object? Error { get; set; }
    }

    public class QuoteSummaryResult
    {
        [JsonPropertyName("defaultKeyStatistics")]
        public DefaultKeyStatistics? DefaultKeyStatistics { get; set; }

        [JsonPropertyName("financialData")]
        public FinancialData? FinancialData { get; set; }

        [JsonPropertyName("summaryDetail")]
        public SummaryDetail? SummaryDetail { get; set; }
    }

    public class DefaultKeyStatistics
    {
        [JsonPropertyName("trailingPE")]
        public YahooRawValue? TrailingPE { get; set; }

        [JsonPropertyName("forwardPE")]
        public YahooRawValue? ForwardPE { get; set; }

        [JsonPropertyName("pegRatio")]
        public YahooRawValue? PegRatio { get; set; }

        [JsonPropertyName("priceToBook")]
        public YahooRawValue? PriceToBook { get; set; }

        [JsonPropertyName("priceToSalesTrailing12Months")]
        public YahooRawValue? PriceToSalesTrailing12Months { get; set; }

        [JsonPropertyName("enterpriseValue")]
        public YahooRawValue? EnterpriseValue { get; set; }

        [JsonPropertyName("enterpriseToEbitda")]
        public YahooRawValue? EnterpriseToEbitda { get; set; }

        [JsonPropertyName("trailingEps")]
        public YahooRawValue? TrailingEps { get; set; }

        [JsonPropertyName("forwardEps")]
        public YahooRawValue? ForwardEps { get; set; }
    }

    public class FinancialData
    {
        [JsonPropertyName("profitMargins")]
        public YahooRawValue? ProfitMargins { get; set; }

        [JsonPropertyName("operatingMargins")]
        public YahooRawValue? OperatingMargins { get; set; }

        [JsonPropertyName("returnOnEquity")]
        public YahooRawValue? ReturnOnEquity { get; set; }

        [JsonPropertyName("returnOnAssets")]
        public YahooRawValue? ReturnOnAssets { get; set; }

        [JsonPropertyName("revenueGrowth")]
        public YahooRawValue? RevenueGrowth { get; set; }

        [JsonPropertyName("earningsGrowth")]
        public YahooRawValue? EarningsGrowth { get; set; }

        [JsonPropertyName("currentRatio")]
        public YahooRawValue? CurrentRatio { get; set; }

        [JsonPropertyName("debtToEquity")]
        public YahooRawValue? DebtToEquity { get; set; }

        [JsonPropertyName("quickRatio")]
        public YahooRawValue? QuickRatio { get; set; }
    }

    public class SummaryDetail
    {
        [JsonPropertyName("dividendYield")]
        public YahooRawValue? DividendYield { get; set; }

        [JsonPropertyName("payoutRatio")]
        public YahooRawValue? PayoutRatio { get; set; }
    }

    /// <summary>
    /// Yahoo Finance returns values in a "raw" and "fmt" format
    /// We use the raw value for calculations
    /// </summary>
    public class YahooRawValue
    {
        [JsonPropertyName("raw")]
        public double? Raw { get; set; }

        [JsonPropertyName("fmt")]
        public string? Fmt { get; set; }
    }

    // Yahoo Finance Company Profile API response models
    public class YahooProfileResponse
    {
        [JsonPropertyName("quoteSummary")]
        public ProfileQuoteSummary QuoteSummary { get; set; } = new();
    }

    public class ProfileQuoteSummary
    {
        [JsonPropertyName("result")]
        public List<ProfileQuoteSummaryResult> Result { get; set; } = new();

        [JsonPropertyName("error")]
        public object? Error { get; set; }
    }

    public class ProfileQuoteSummaryResult
    {
        [JsonPropertyName("assetProfile")]
        public AssetProfile? AssetProfile { get; set; }

        [JsonPropertyName("summaryProfile")]
        public SummaryProfile? SummaryProfile { get; set; }

        [JsonPropertyName("price")]
        public PriceInfo? Price { get; set; }
    }

    public class AssetProfile
    {
        [JsonPropertyName("address1")]
        public string? Address1 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("sector")]
        public string? Sector { get; set; }

        [JsonPropertyName("longBusinessSummary")]
        public string? LongBusinessSummary { get; set; }

        [JsonPropertyName("fullTimeEmployees")]
        public int? FullTimeEmployees { get; set; }

        [JsonPropertyName("companyOfficers")]
        public List<CompanyOfficer>? CompanyOfficers { get; set; }
    }

    public class SummaryProfile
    {
        [JsonPropertyName("address1")]
        public string? Address1 { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("sector")]
        public string? Sector { get; set; }
    }

    public class CompanyOfficer
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [JsonPropertyName("yearBorn")]
        public int? YearBorn { get; set; }
    }

    public class PriceInfo
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("longName")]
        public string? LongName { get; set; }

        [JsonPropertyName("exchangeName")]
        public string? ExchangeName { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }
    }

    // Yahoo Finance Search API response models
    public class YahooSearchResponse
    {
        [JsonPropertyName("quotes")]
        public List<YahooSearchQuote> Quotes { get; set; } = new();

        [JsonPropertyName("news")]
        public List<object> News { get; set; } = new();

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class YahooSearchQuote
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("shortname")]
        public string? ShortName { get; set; }

        [JsonPropertyName("longname")]
        public string? LongName { get; set; }

        [JsonPropertyName("exchange")]
        public string? Exchange { get; set; }

        [JsonPropertyName("exchDisp")]
        public string? ExchDisp { get; set; }

        [JsonPropertyName("quoteType")]
        public string? QuoteType { get; set; }

        [JsonPropertyName("typeDisp")]
        public string? TypeDisp { get; set; }

        [JsonPropertyName("sector")]
        public string? Sector { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("isYahooFinance")]
        public bool IsYahooFinance { get; set; }
    }
}
