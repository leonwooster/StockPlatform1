using Microsoft.AspNetCore.Mvc;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Enums;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StocksController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StocksController> _logger;

        public StocksController(IStockService stockService, ILogger<StocksController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
        {
            var stocks = await _stockService.GetAllStocksAsync();
            return Ok(stocks);
        }

        [HttpGet("{symbol}")]
        public async Task<ActionResult<Stock>> GetStock(string symbol)
        {
            var stock = await _stockService.GetStockBySymbolAsync(symbol);
            if (stock == null)
            {
                return NotFound();
            }
            return Ok(stock);
        }

        /// <summary>
        /// Gets current market data (quote) for a stock symbol
        /// </summary>
        /// <param name="symbol">The stock symbol (e.g., AAPL)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Market data including current price, volume, and other metrics</returns>
        [HttpGet("{symbol}/quote")]
        [ProducesResponseType(typeof(MarketData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MarketData>> GetQuote(string symbol, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching quote for symbol: {Symbol}", symbol);
                var marketData = await _stockService.GetQuoteAsync(symbol, cancellationToken);
                return Ok(marketData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quote for symbol: {Symbol}", symbol);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets historical price data for a stock symbol
        /// </summary>
        /// <param name="symbol">The stock symbol (e.g., AAPL)</param>
        /// <param name="startDate">Start date for historical data (format: yyyy-MM-dd)</param>
        /// <param name="endDate">End date for historical data (format: yyyy-MM-dd)</param>
        /// <param name="interval">Time interval: Daily, Weekly, or Monthly (default: Daily)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of historical prices (OHLCV data)</returns>
        [HttpGet("{symbol}/historical")]
        [ProducesResponseType(typeof(List<StockPrice>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<StockPrice>>> GetHistoricalPrices(
            string symbol,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] TimeInterval interval = TimeInterval.Daily,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Default to last 90 days if no date range specified
                var start = startDate ?? DateTime.UtcNow.AddDays(-90);
                var end = endDate ?? DateTime.UtcNow;

                _logger.LogInformation(
                    "Fetching historical prices for symbol: {Symbol}, StartDate: {StartDate}, EndDate: {EndDate}, Interval: {Interval}",
                    symbol,
                    start.ToString("yyyy-MM-dd"),
                    end.ToString("yyyy-MM-dd"),
                    interval);

                var historicalPrices = await _stockService.GetHistoricalPricesAsync(symbol, start, end, interval, cancellationToken);
                return Ok(historicalPrices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical prices for symbol: {Symbol}", symbol);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets fundamental data for a stock symbol
        /// </summary>
        /// <param name="symbol">The stock symbol (e.g., AAPL)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Fundamental data including financial ratios and metrics</returns>
        [HttpGet("{symbol}/fundamentals")]
        [ProducesResponseType(typeof(FundamentalData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FundamentalData>> GetFundamentals(string symbol, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching fundamentals for symbol: {Symbol}", symbol);
                var fundamentalData = await _stockService.GetFundamentalsAsync(symbol, cancellationToken);
                return Ok(fundamentalData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fundamentals for symbol: {Symbol}", symbol);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets company profile information for a stock symbol
        /// </summary>
        /// <param name="symbol">The stock symbol (e.g., AAPL)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Company profile including name, sector, industry, and description</returns>
        [HttpGet("{symbol}/profile")]
        [ProducesResponseType(typeof(CompanyProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CompanyProfile>> GetProfile(string symbol, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching profile for symbol: {Symbol}", symbol);
                var companyProfile = await _stockService.GetCompanyProfileAsync(symbol, cancellationToken);
                return Ok(companyProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for symbol: {Symbol}", symbol);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Searches for stocks by company name or symbol
        /// </summary>
        /// <param name="query">Search query (company name or symbol)</param>
        /// <param name="limit">Maximum number of results to return (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching stock search results</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<StockSearchResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<StockSearchResult>>> SearchSymbols(
            [FromQuery] string query,
            [FromQuery] int limit = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { error = "Query parameter is required" });
                }

                if (limit < 1 || limit > 50)
                {
                    return BadRequest(new { error = "Limit must be between 1 and 50" });
                }

                _logger.LogInformation("Searching symbols with query: {Query}, limit: {Limit}", query, limit);
                var searchResults = await _stockService.SearchSymbolsAsync(query, limit, cancellationToken);
                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching symbols with query: {Query}", query);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Stock>> CreateStock(Stock stock)
        {
            var createdStock = await _stockService.CreateStockAsync(stock);
            return CreatedAtAction(nameof(GetStock), new { symbol = createdStock.Symbol }, createdStock);
        }

        [HttpPut("{symbol}")]
        public async Task<IActionResult> UpdateStock(string symbol, Stock stock)
        {
            if (symbol != stock.Symbol)
            {
                return BadRequest();
            }

            await _stockService.UpdateStockAsync(stock);
            return NoContent();
        }

        [HttpDelete("{symbol}")]
        public async Task<IActionResult> DeleteStock(string symbol)
        {
            var result = await _stockService.DeleteStockAsync(symbol);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
