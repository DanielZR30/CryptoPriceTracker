namespace CryptoPriceTraker.Modules.Track.Models
{
    public class CryptoAssetResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal ChangePercentage { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}