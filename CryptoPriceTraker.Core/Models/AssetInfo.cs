namespace CryptoPriceTraker.Core.Models
{
    public class AssetInfo
    {
        public string ExternalId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string? IconUrl { get; set; }
        public decimal? LastPrice { get; set; }
    }
}