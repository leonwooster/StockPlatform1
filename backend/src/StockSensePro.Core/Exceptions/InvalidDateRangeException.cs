namespace StockSensePro.Core.Exceptions
{
    public class InvalidDateRangeException : StockDataException
    {
        public InvalidDateRangeException() : base()
        {
            Code = ErrorCode.InvalidDateRange;
        }

        public InvalidDateRangeException(string message) 
            : base(message, string.Empty, ErrorCode.InvalidDateRange)
        {
        }

        public InvalidDateRangeException(string message, string symbol) 
            : base(message, symbol, ErrorCode.InvalidDateRange)
        {
        }

        public InvalidDateRangeException(string message, Exception innerException) 
            : base(message, string.Empty, ErrorCode.InvalidDateRange, innerException)
        {
        }
    }
}
