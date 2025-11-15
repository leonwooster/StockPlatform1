namespace StockSensePro.Core.Entities
{
    public class CompanyProfile
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int? EmployeeCount { get; set; }
        public string CEO { get; set; } = string.Empty;
        public DateTime? FoundedYear { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }
}
