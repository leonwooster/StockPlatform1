using Microsoft.AspNetCore.Mvc;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StocksController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StocksController(IStockService stockService)
        {
            _stockService = stockService;
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
