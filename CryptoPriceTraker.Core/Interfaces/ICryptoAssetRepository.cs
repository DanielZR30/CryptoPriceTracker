using CryptoPriceTraker.Core.Models;

namespace CryptoPriceTraker.Core.Interfaces
{
    public interface ICryptoAssetRepository
    {
        Task<IEnumerable<CryptoAsset>> GetAllAsync();
        Task<CryptoAsset?> GetByIdAsync(int id);
        Task<CryptoAsset?> GetByExternalIdAsync(string externalId);
        Task<List<CryptoAsset>> GetByExternalIdsAsync(IEnumerable<string> externalIds);
        Task<List<AssetInfo>> GetAssetsInfoAsync(IEnumerable<string> externalIds);
        Task<Dictionary<int, CryptoPriceHistory>> GetLastPriceHistoriesAsync(IEnumerable<int> assetIds);
        Task<Dictionary<int, List<CryptoPriceHistory>>> GetTwoLatestPriceHistoriesAsync(IEnumerable<int> assetIds);
        Task<CryptoPriceHistory?> GetLastPriceHistoryAsync(int assetId);
        void AddPriceHistory(CryptoPriceHistory priceHistory);
        void AddPriceHistories(IEnumerable<CryptoPriceHistory> priceHistories);
        void AddAsset(CryptoAsset asset);
        void UpdateAsset(CryptoAsset asset);
        void UpdateAssets(IEnumerable<CryptoAsset> assets);
    }
}