namespace StockSensePro.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when an invalid API key is used
    /// </summary>
    public class InvalidApiKeyException : StockDataException
    {
        public string Provider { get; set; } = string.Empty;

        public InvalidApiKeyException() : base()
        {
            Code = ErrorCode.ApiUnavailable;
        }

        public InvalidApiKeyException(string message) 
            : base(message, string.Empty, ErrorCode.ApiUnavailable)
        {
        }

        public InvalidApiKeyException(string message, string provider) 
            : base(message, string.Empty, ErrorCode.ApiUnavailable)
        {
            Provider = provider;
        }

        public InvalidApiKeyException(string message, string provider, Exception innerException) 
            : base(message, string.Empty, ErrorCode.ApiUnavailable, innerException)
        {
            Provider = provider;
        }
    }
}
