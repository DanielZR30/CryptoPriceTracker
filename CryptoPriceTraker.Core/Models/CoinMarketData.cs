using System.Text.Json.Serialization;

namespace CryptoPriceTraker.Core.Models
{
    public class CoinMarketData
    {
        public string Id { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        [JsonPropertyName("current_price")]
        public decimal CurrentPrice { get; set; }
    }
}
