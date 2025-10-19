using StockSensePro.Core.Entities;

namespace StockSensePro.Core.Interfaces
{
    public interface IStockService
    {
        Task<Stock?> GetStockBySymbolAsync(string symbol);
        Task<IEnumerable<Stock>> GetAllStocksAsync();
        Task<Stock> CreateStockAsync(Stock stock);
        Task<Stock> UpdateStockAsync(Stock stock);
        Task<bool> DeleteStockAsync(string symbol);
    }
}
