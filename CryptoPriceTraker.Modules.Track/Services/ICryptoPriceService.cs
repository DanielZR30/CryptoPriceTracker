using CryptoPriceTraker.Core.Models;
using CryptoPriceTraker.Modules.Track.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoPriceTraker.Modules.Track.Services
{
    public interface ICryptoPriceService
    {
        Task<List<CryptoAssetResponseDto>> UpdatePricesAsync();
        Task<PaginatedResult<CryptoAssetResponseDto>> GetLatestPricesAsync(int pageNumber, int pageSize);
    }
}
