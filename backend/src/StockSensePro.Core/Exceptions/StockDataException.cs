namespace StockSensePro.Core.Exceptions
{
    public enum ErrorCode
    {
        Unknown,
        SymbolNotFound,
        RateLimitExceeded,
        ApiUnavailable,
        InvalidDateRange,
        InvalidParameter,
        Timeout
    }

    public class StockDataException : Exception
    {
        public string Symbol { get; set; } = string.Empty;
        public ErrorCode Code { get; set; }

        public StockDataException() : base()
        {
        }

        public StockDataException(string message) : base(message)
        {
        }

        public StockDataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public StockDataException(string message, string symbol, ErrorCode code) : base(message)
        {
            Symbol = symbol;
            Code = code;
        }

        public StockDataException(string message, string symbol, ErrorCode code, Exception innerException) 
            : base(message, innerException)
        {
            Symbol = symbol;
            Code = code;
        }
    }
}
