using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoPriceTraker.Core.Models;

[Table("CryptoAssets")]
public class CryptoAsset
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string ExternalId { get; set; }
    public string? IconUrl { get; set; }
    public ICollection<CryptoPriceHistory> PriceHistory { get; set; } = new List<CryptoPriceHistory>();
}