using StockSensePro.Core.Enums;

namespace StockSensePro.Core.Interfaces
{
    /// <summary>
    /// Interface for calculating API usage costs per provider
    /// </summary>
    public interface IProviderCostCalculator
    {
        /// <summary>
        /// Gets the cost per API call for a specific provider
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>Cost per API call in USD</returns>
        decimal GetCostPerCall(DataProviderType provider);

        /// <summary>
        /// Calculates the estimated cost for a given number of API calls
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <param name="callCount">Number of API calls</param>
        /// <returns>Estimated cost in USD</returns>
        decimal CalculateCost(DataProviderType provider, long callCount);

        /// <summary>
        /// Gets the monthly subscription cost for a provider (if applicable)
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <returns>Monthly subscription cost in USD, or 0 if free</returns>
        decimal GetMonthlySubscriptionCost(DataProviderType provider);

        /// <summary>
        /// Gets the total estimated cost including subscription and usage
        /// </summary>
        /// <param name="provider">The provider type</param>
        /// <param name="callCount">Number of API calls</param>
        /// <returns>Total estimated cost in USD</returns>
        decimal GetTotalEstimatedCost(DataProviderType provider, long callCount);
    }
}
