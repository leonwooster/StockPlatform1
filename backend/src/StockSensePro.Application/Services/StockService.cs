using Microsoft.Extensions.Logging;
using StockSensePro.Core.Entities;
using StockSensePro.Core.Interfaces;

namespace StockSensePro.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly ILogger<StockService> _logger;

        public StockService(IStockRepository stockRepository, ILogger<StockService> logger)
        {
            _stockRepository = stockRepository;
            _logger = logger;
        }

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
    }
}
