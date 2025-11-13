using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StockSensePro.Core.Entities;

namespace StockSensePro.Infrastructure.Services
{
    public interface IYahooFinanceService
    {
        Task<Stock?> GetStockAsync(string symbol, CancellationToken cancellationToken = default);
        Task<List<StockPrice>> GetHistoricalPricesAsync(string symbol, int days = 30, CancellationToken cancellationToken = default);
        Task<List<StockPrice>> GetHistoricalPricesAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<CompanyInfo?> GetCompanyInfoAsync(string symbol, CancellationToken cancellationToken = default);
    }

    public class YahooFinanceService : IYahooFinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YahooFinanceService> _logger;

        public YahooFinanceService(HttpClient httpClient, ILogger<YahooFinanceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://query1.finance.yahoo.com/v8/finance/chart/");
        }

        public async Task<Stock?> GetStockAsync(string symbol, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{symbol}", cancellationToken);
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
                _logger.LogError(ex, "Error fetching stock data for symbol {Symbol}", symbol);
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
            try
            {
                if (startDate >= endDate)
                {
                    throw new ArgumentException("Start date must be earlier than end date.");
                }

                var response = await _httpClient.GetAsync(
                    $"{symbol}?period1={ToUnixTimestamp(startDate)}&period2={ToUnixTimestamp(endDate)}&interval=1d",
                    cancellationToken);
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

                return prices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical prices for symbol {Symbol}", symbol);
                return new List<StockPrice>();
            }
        }

        public async Task<CompanyInfo?> GetCompanyInfoAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would fetch company info from Yahoo Finance
            // For now, we'll return null to indicate this feature needs implementation
            return await Task.FromResult<CompanyInfo?>(null);
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

    // Yahoo Finance API response models
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
}
