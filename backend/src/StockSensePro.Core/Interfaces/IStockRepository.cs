using StockSensePro.Core.Entities;

namespace StockSensePro.Core.Interfaces
{
    public interface IStockRepository
    {
        Task<Stock?> GetBySymbolAsync(string symbol);
        Task<IEnumerable<Stock>> GetAllAsync();
        Task AddAsync(Stock stock);
        Task UpdateAsync(Stock stock);
        Task DeleteAsync(string symbol);
    }
}
