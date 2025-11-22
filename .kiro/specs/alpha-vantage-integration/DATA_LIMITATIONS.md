# Alpha Vantage Data Limitations and Workarounds

## Overview

This document outlines the data fields available in Yahoo Finance but not directly available in Alpha Vantage's free tier, along with recommended workarounds and implementation strategies.

## Data Availability Comparison

### ✅ Fully Available in Alpha Vantage

| Data Field | Alpha Vantage Endpoint | Yahoo Finance | Notes |
|------------|------------------------|---------------|-------|
| Current Price | Global Quote | ✅ | ✅ Direct mapping |
| Open/High/Low/Close | Global Quote | ✅ | ✅ Direct mapping |
| Volume | Global Quote | ✅ | ✅ Direct mapping |
| Previous Close | Global Quote | ✅ | ✅ Direct mapping |
| Change/Change % | Global Quote | ✅ | ✅ Direct mapping |
| Historical OHLCV | Time Series Daily | ✅ | ✅ Direct mapping |
| Company Name | Company Overview | ✅ | ✅ Direct mapping |
| Sector/Industry | Company Overview | ✅ | ✅ Direct mapping |
| Market Cap | Company Overview | ✅ | ✅ Direct mapping |
| P/E Ratio | Company Overview | ✅ | ✅ Direct mapping |
| EPS | Company Overview | ✅ | ✅ Direct mapping |
| Dividend Yield | Company Overview | ✅ | ✅ Direct mapping |

### ⚠️ Requires Calculation/Workaround

| Data Field | Alpha Vantage | Yahoo Finance | Workaround |
|------------|---------------|---------------|------------|
| 52-Week High | ❌ Not direct | ✅ Direct | Calculate from 1-year historical data |
| 52-Week Low | ❌ Not direct | ✅ Direct | Calculate from 1-year historical data |
| Average Volume | ❌ Not direct | ✅ Direct | Calculate from 30-day historical data |
| Day Range | ⚠️ Partial | ✅ Direct | Use High/Low from Global Quote |

### ❌ Not Available in Free Tier

| Data Field | Alpha Vantage Free | Alpha Vantage Premium | Yahoo Finance | Recommendation |
|------------|-------------------|----------------------|---------------|----------------|
| Bid Price | ❌ | ✅ | ✅ | Use Yahoo Finance or set to null |
| Ask Price | ❌ | ✅ | ✅ | Use Yahoo Finance or set to null |
| Bid Size | ❌ | ✅ | ✅ | Use Yahoo Finance or set to null |
| Ask Size | ❌ | ✅ | ✅ | Use Yahoo Finance or set to null |
| Pre-Market Price | ❌ | ⚠️ Limited | ✅ | Use Yahoo Finance or set to null |
| After-Hours Price | ❌ | ⚠️ Limited | ✅ | Use Yahoo Finance or set to null |
| Options Data | ❌ | ❌ | ✅ | Use Yahoo Finance only |
| Real-time News | ❌ | ✅ | ✅ | Use Yahoo Finance or separate news API |

## Recommended Implementation Strategies

### Strategy 1: Hybrid Approach (Recommended)

Use Alpha Vantage for core data and supplement with Yahoo Finance for missing fields.

```csharp
public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
{
    // Get core data from Alpha Vantage (reliable, cached)
    var alphaVantageQuote = await _alphaVantageService.GetQuoteAsync(symbol, cancellationToken);
    
    // Supplement with Yahoo Finance for bid/ask if needed
    if (_settings.IncludeBidAskPrices)
    {
        try
        {
            var yahooQuote = await _yahooFinanceService.GetQuoteAsync(symbol, cancellationToken);
            alphaVantageQuote.BidPrice = yahooQuote.BidPrice;
            alphaVantageQuote.AskPrice = yahooQuote.AskPrice;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch bid/ask from Yahoo Finance for {Symbol}", symbol);
            // Continue without bid/ask data
        }
    }
    
    return alphaVantageQuote;
}
```

**Pros:**
- ✅ Best of both worlds
- ✅ Reliable core data from Alpha Vantage
- ✅ Complete data coverage
- ✅ Graceful degradation if Yahoo fails

**Cons:**
- ⚠️ Two API calls per quote (mitigated by caching)
- ⚠️ Slightly more complex

### Strategy 2: Calculate Missing Fields

Calculate 52-week high/low and average volume from historical data.

```csharp
public async Task<MarketData> GetQuoteWithCalculatedFieldsAsync(
    string symbol, 
    CancellationToken cancellationToken = default)
{
    // Get current quote
    var quote = await _alphaVantageService.GetQuoteAsync(symbol, cancellationToken);
    
    // Check cache for calculated fields
    var cacheKey = $"calculated:{symbol}";
    var cachedFields = await _cacheService.GetAsync<CalculatedFields>(cacheKey);
    
    if (cachedFields == null)
    {
        // Fetch 1 year of historical data
        var historicalData = await _alphaVantageService.GetHistoricalPricesAsync(
            symbol,
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow,
            TimeInterval.Daily,
            cancellationToken);
        
        // Calculate 52-week high/low
        quote.FiftyTwoWeekHigh = historicalData.Max(p => p.High);
        quote.FiftyTwoWeekLow = historicalData.Min(p => p.Low);
        
        // Calculate average volume (last 30 days)
        var recentData = historicalData.TakeLast(30);
        quote.AverageVolume = (long)recentData.Average(p => p.Volume);
        
        // Cache calculated fields for 24 hours
        cachedFields = new CalculatedFields
        {
            FiftyTwoWeekHigh = quote.FiftyTwoWeekHigh,
            FiftyTwoWeekLow = quote.FiftyTwoWeekLow,
            AverageVolume = quote.AverageVolume
        };
        await _cacheService.SetAsync(cacheKey, cachedFields, TimeSpan.FromHours(24));
    }
    else
    {
        // Use cached calculated fields
        quote.FiftyTwoWeekHigh = cachedFields.FiftyTwoWeekHigh;
        quote.FiftyTwoWeekLow = cachedFields.FiftyTwoWeekLow;
        quote.AverageVolume = cachedFields.AverageVolume;
    }
    
    return quote;
}
```

**Pros:**
- ✅ Single provider (Alpha Vantage only)
- ✅ Accurate calculations
- ✅ Cached for efficiency

**Cons:**
- ⚠️ Initial calculation requires historical data fetch
- ⚠️ Uses more API quota on first request
- ⚠️ 24-hour cache means slightly stale 52-week data

### Strategy 3: Nullable Fields

Set unavailable fields to null and document the limitation.

```csharp
public async Task<MarketData> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
{
    var quote = await _alphaVantageService.GetQuoteAsync(symbol, cancellationToken);
    
    // Set unavailable fields to null
    quote.BidPrice = null;
    quote.AskPrice = null;
    quote.FiftyTwoWeekHigh = null;
    quote.FiftyTwoWeekLow = null;
    quote.AverageVolume = null;
    
    return quote;
}
```

**Pros:**
- ✅ Simple implementation
- ✅ Single provider
- ✅ Clear about data availability

**Cons:**
- ❌ Incomplete data
- ❌ May break UI expectations
- ❌ Less useful for users

## Recommended Configuration

### Configuration Model

Add settings to control data enrichment behavior:

```csharp
public class AlphaVantageSettings
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
    public bool Enabled { get; set; } = false;
    
    // Data enrichment settings
    public DataEnrichmentSettings Enrichment { get; set; } = new();
}

public class DataEnrichmentSettings
{
    // Use Yahoo Finance to supplement bid/ask prices
    public bool IncludeBidAskPrices { get; set; } = true;
    
    // Calculate 52-week high/low from historical data
    public bool Calculate52WeekRange { get; set; } = true;
    
    // Calculate average volume from historical data
    public bool CalculateAverageVolume { get; set; } = true;
    
    // Cache duration for calculated fields (hours)
    public int CalculatedFieldsCacheDuration { get; set; } = 24;
}
```

### appsettings.json Example

```json
{
  "AlphaVantage": {
    "ApiKey": "",
    "Enabled": true,
    "Enrichment": {
      "IncludeBidAskPrices": true,
      "Calculate52WeekRange": true,
      "CalculateAverageVolume": true,
      "CalculatedFieldsCacheDuration": 24
    }
  },
  "DataProvider": {
    "PrimaryProvider": "AlphaVantage",
    "FallbackProvider": "YahooFinance",
    "Strategy": "Fallback"
  }
}
```

## API Quota Impact Analysis

### Without Enrichment
- **Quote request**: 1 API call to Alpha Vantage
- **Daily quota usage**: ~25 quotes per day (free tier)

### With Bid/Ask Enrichment (Hybrid)
- **Quote request**: 1 Alpha Vantage + 1 Yahoo Finance
- **Daily quota usage**: ~25 quotes per day (Alpha Vantage), unlimited (Yahoo)
- **Cache hit rate**: 80%+ means only ~5 actual API calls per day

### With Calculated Fields
- **First quote request**: 2 API calls (quote + historical)
- **Subsequent requests**: 1 API call (cached calculated fields)
- **Daily quota usage**: ~15-20 quotes per day (accounting for historical fetches)

### Recommended Approach
Use **Hybrid Strategy** with aggressive caching:

```
1. Check cache for quote (15-min TTL)
2. If miss, fetch from Alpha Vantage
3. Check cache for bid/ask (5-min TTL)
4. If miss, fetch from Yahoo Finance
5. Check cache for calculated fields (24-hour TTL)
6. If miss, calculate from historical data
```

**Expected API usage:**
- Alpha Vantage: 5-10 calls/day (with 80% cache hit rate)
- Yahoo Finance: 10-15 calls/day (for bid/ask only)
- **Total cost**: $0/month (within free tiers)

## Updated Requirements

### New Requirement: Data Enrichment

**User Story:** As a user, I want complete market data even when using Alpha Vantage, so that I have all the information I need for trading decisions.

#### Acceptance Criteria

1. WHEN Alpha Vantage is the primary provider, THE System SHALL optionally supplement data with Yahoo Finance for bid/ask prices
2. WHEN 52-week high/low is requested, THE System SHALL calculate from historical data if not directly available
3. WHEN average volume is requested, THE System SHALL calculate from recent historical data if not directly available
4. THE System SHALL cache calculated fields separately with longer TTL to minimize API usage
5. THE System SHALL allow configuration of data enrichment behavior per field type

### Updated Requirement 4: Data Model Mapping

Add acceptance criteria:

6. WHEN Alpha Vantage does not provide a field, THE System SHALL set the field to null OR calculate from available data OR supplement from fallback provider based on configuration
7. THE System SHALL log when data enrichment is used
8. THE System SHALL track API usage for enrichment operations separately

## Implementation Priority

### Phase 1: Core Implementation (Must Have)
1. ✅ Basic Alpha Vantage integration
2. ✅ Direct field mapping (price, volume, etc.)
3. ✅ Nullable fields for unavailable data

### Phase 2: Calculated Fields (Should Have)
1. ⚠️ 52-week high/low calculation
2. ⚠️ Average volume calculation
3. ⚠️ Caching for calculated fields

### Phase 3: Hybrid Enrichment (Nice to Have)
1. 💡 Bid/ask from Yahoo Finance
2. 💡 Configurable enrichment
3. 💡 Enrichment metrics tracking

## Testing Considerations

### Unit Tests
- Test data mapping with missing fields
- Test calculation logic for 52-week range
- Test calculation logic for average volume
- Test hybrid enrichment with mock providers
- Test cache behavior for calculated fields

### Integration Tests
- Test with real Alpha Vantage API
- Test enrichment with real Yahoo Finance API
- Test cache hit/miss scenarios
- Test API quota usage

### Performance Tests
- Measure response time with/without enrichment
- Measure cache effectiveness
- Measure API quota consumption

## Migration Notes

### For Existing Yahoo Finance Users

**No Breaking Changes:**
- All fields remain in MarketData model
- Fields not available from Alpha Vantage will be null or calculated
- UI should handle null values gracefully

**Recommended UI Updates:**
```typescript
// Frontend should handle nullable fields
interface MarketData {
  currentPrice: number;
  bidPrice?: number | null;  // May be null with Alpha Vantage
  askPrice?: number | null;   // May be null with Alpha Vantage
  fiftyTwoWeekHigh?: number | null;  // May be null or calculated
  fiftyTwoWeekLow?: number | null;   // May be null or calculated
}

// Display with fallback
<div>
  Bid: {quote.bidPrice ?? 'N/A'}
  Ask: {quote.askPrice ?? 'N/A'}
</div>
```

## Conclusion

**Recommended Approach:**
1. **Start with Strategy 1 (Hybrid)** for complete data coverage
2. **Enable aggressive caching** to minimize API usage
3. **Monitor API quota** and adjust strategy if needed
4. **Make enrichment configurable** so users can optimize for their needs

**Expected Outcome:**
- ✅ Complete data coverage
- ✅ High reliability (Alpha Vantage primary)
- ✅ Low cost (within free tiers)
- ✅ Good performance (80%+ cache hit rate)
- ✅ Backward compatible (no breaking changes)

