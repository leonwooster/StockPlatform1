using Microsoft.Extensions.Logging;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Infrastructure.Services;

/// <summary>
/// Mock implementation of Yahoo Finance service for development/testing
/// Returns realistic fake data without making actual API calls
/// </summary>
public class MockYahooFinanceService : IYahooFinanceService
{
    private readonly ILogger<MockYahooFinanceService> _logger;
    private readonly Random _random = new();
    private readonly Dictionary<string, decimal> _basePrices = new()
    {
        { "AAPL", 175.00m },
        { "MSFT", 380.00m },
        { "GOOGL", 140.00m },
        { "AMZN", 155.00m },
        { "NVDA", 495.00m },
        { "TSLA", 245.00m },
        { "META", 350.00m },
        { "AMD", 145.00m },
        { "SMCI", 900.00m },
        { "^GSPC", 4950.00m },  // S&P 500
        { "^NDX", 15300.00m },  // NASDAQ 100
        { "^DJI", 37500.00m }   // Dow Jones
    };

    public MockYahooFinanceService(ILogger<MockYahooFinanceService> logger)
    {
        _logger = logger;
    }

    public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate network delay

        _logger.LogInformation("Mock: Fetching quote for {Symbol}", symbol);

        var basePrice = _basePrices.GetValueOrDefault(symbol, 100.00m);
        var priceVariation = (decimal)(_random.NextDouble() * 10 - 5); // -5 to +5
        var currentPrice = basePrice + priceVariation;
        var change = priceVariation;
        var changePercent = (change / basePrice) * 100;

        return new MarketData
        {
            Symbol = symbol,
            ShortName = GetCompanyName(symbol),
            LongName = GetCompanyName(symbol),
            CurrentPrice = Math.Round(currentPrice, 2),
            PreviousClose = Math.Round(basePrice, 2),
            Open = Math.Round(basePrice + (decimal)(_random.NextDouble() * 2 - 1), 2),
            DayHigh = Math.Round(currentPrice + (decimal)(_random.NextDouble() * 3), 2),
            DayLow = Math.Round(currentPrice - (decimal)(_random.NextDouble() * 3), 2),
            Change = Math.Round(change, 2),
            ChangePercent = Math.Round(changePercent, 2),
            Volume = _random.Next(10000000, 100000000),
            MarketCap = (long)(currentPrice * _random.Next(1000000000, 3000000000)),
            Timestamp = DateTime.UtcNow,
            MarketState = MarketState.Regular
        };
    }

    public async Task<List<MarketData>> GetQuotesAsync(List<string> symbols, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Fetching quotes for {Count} symbols", symbols.Count);
        
        var tasks = symbols.Select(symbol => GetQuoteAsync(symbol, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<List<StockPrice>> GetHistoricalPricesAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        TimeInterval interval = TimeInterval.Daily,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken);

        _logger.LogInformation("Mock: Fetching historical prices for {Symbol} from {StartDate} to {EndDate}",
            symbol, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        var prices = new List<StockPrice>();
        var basePrice = _basePrices.GetValueOrDefault(symbol, 100.00m);
        var currentDate = startDate;
        var currentPrice = basePrice;

        while (currentDate <= endDate)
        {
            // Skip weekends
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                var dailyChange = (decimal)(_random.NextDouble() * 6 - 3); // -3% to +3%
                currentPrice += dailyChange;
                
                var open = currentPrice + (decimal)(_random.NextDouble() * 2 - 1);
                var high = Math.Max(open, currentPrice) + (decimal)(_random.NextDouble() * 2);
                var low = Math.Min(open, currentPrice) - (decimal)(_random.NextDouble() * 2);

                prices.Add(new StockPrice
                {
                    Symbol = symbol,
                    Date = currentDate,
                    Open = Math.Round(open, 2),
                    High = Math.Round(high, 2),
                    Low = Math.Round(low, 2),
                    Close = Math.Round(currentPrice, 2),
                    Volume = _random.Next(10000000, 100000000)
                });
            }

            currentDate = interval switch
            {
                TimeInterval.Daily => currentDate.AddDays(1),
                TimeInterval.Weekly => currentDate.AddDays(7),
                TimeInterval.Monthly => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };
        }

        return prices;
    }

    public async Task<FundamentalData> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        _logger.LogInformation("Mock: Fetching fundamentals for {Symbol}", symbol);

        var basePrice = _basePrices.GetValueOrDefault(symbol, 100.00m);

        return new FundamentalData
        {
            Symbol = symbol,
            MarketCap = (long)(basePrice * _random.Next(1000000000, 3000000000)),
            PeRatio = (decimal)(_random.NextDouble() * 30 + 10),
            Eps = Math.Round(basePrice / (decimal)(_random.NextDouble() * 30 + 10), 2),
            DividendYield = (decimal)(_random.NextDouble() * 0.03),
            Beta = (decimal)(_random.NextDouble() * 0.5 + 0.8),
            FiftyTwoWeekHigh = Math.Round(basePrice * 1.2m, 2),
            FiftyTwoWeekLow = Math.Round(basePrice * 0.8m, 2),
            FiftyDayAverage = Math.Round(basePrice * 0.98m, 2),
            TwoHundredDayAverage = Math.Round(basePrice * 0.95m, 2),
            ProfitMargin = (decimal)(_random.NextDouble() * 0.25),
            OperatingMargin = (decimal)(_random.NextDouble() * 0.30),
            ReturnOnEquity = (decimal)(_random.NextDouble() * 0.20),
            RevenuePerShare = Math.Round(basePrice * (decimal)(_random.NextDouble() * 2), 2),
            QuarterlyRevenueGrowth = (decimal)(_random.NextDouble() * 0.15),
            QuarterlyEarningsGrowth = (decimal)(_random.NextDouble() * 0.20)
        };
    }

    public async Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        _logger.LogInformation("Mock: Fetching company profile for {Symbol}", symbol);

        return new CompanyProfile
        {
            Symbol = symbol,
            Name = GetCompanyName(symbol),
            Sector = GetSector(symbol),
            Industry = GetIndustry(symbol),
            Description = $"{GetCompanyName(symbol)} is a leading company in the {GetSector(symbol)} sector, specializing in {GetIndustry(symbol)}. The company has a strong market presence and continues to innovate in its field.",
            Website = $"https://www.{symbol.ToLower().Replace("^", "")}.com",
            FullTimeEmployees = _random.Next(10000, 200000),
            City = "Cupertino",
            State = "CA",
            Country = "United States",
            Phone = "+1-555-0100"
        };
    }

    public async Task<List<StockSearchResult>> SearchSymbolsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);

        _logger.LogInformation("Mock: Searching symbols with query: {Query}", query);

        var allSymbols = _basePrices.Keys.ToList();
        var results = allSymbols
            .Where(s => s.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                       GetCompanyName(s).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(symbol => new StockSearchResult
            {
                Symbol = symbol,
                Name = GetCompanyName(symbol),
                Exchange = "NASDAQ",
                AssetType = "Stock"
            })
            .ToList();

        return results;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        _logger.LogInformation("Mock: Health check - always healthy");
        return true; // Mock service is always healthy
    }

    private string GetCompanyName(string symbol)
    {
        return symbol switch
        {
            "AAPL" => "Apple Inc.",
            "MSFT" => "Microsoft Corporation",
            "GOOGL" => "Alphabet Inc.",
            "AMZN" => "Amazon.com Inc.",
            "NVDA" => "NVIDIA Corporation",
            "TSLA" => "Tesla Inc.",
            "META" => "Meta Platforms Inc.",
            "AMD" => "Advanced Micro Devices Inc.",
            "SMCI" => "Super Micro Computer Inc.",
            "^GSPC" => "S&P 500 Index",
            "^NDX" => "NASDAQ 100 Index",
            "^DJI" => "Dow Jones Industrial Average",
            _ => $"{symbol} Company"
        };
    }

    private string GetSector(string symbol)
    {
        return symbol switch
        {
            "AAPL" or "MSFT" or "GOOGL" => "Technology",
            "AMZN" => "Consumer Cyclical",
            "NVDA" or "AMD" or "SMCI" => "Technology",
            "TSLA" => "Consumer Cyclical",
            "META" => "Communication Services",
            _ => "Technology"
        };
    }

    private string GetIndustry(string symbol)
    {
        return symbol switch
        {
            "AAPL" => "Consumer Electronics",
            "MSFT" => "Software",
            "GOOGL" => "Internet Content & Information",
            "AMZN" => "Internet Retail",
            "NVDA" or "AMD" => "Semiconductors",
            "TSLA" => "Auto Manufacturers",
            "META" => "Internet Content & Information",
            "SMCI" => "Computer Hardware",
            _ => "Technology"
        };
    }
}
