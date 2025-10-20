using System.Text.Json.Serialization;

namespace CryptoPriceTracker.Infrastructure.Models
{
    public class CoinMarketData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("current_price")]
        public decimal CurrentPrice { get; set; }
    }
}
