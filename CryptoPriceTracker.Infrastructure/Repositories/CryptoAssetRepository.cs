using CryptoPriceTraker.Core.Interfaces;
using CryptoPriceTraker.Core.Models;
using CryptoPriceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoPriceTracker.Infrastructure.Repositories
{
    public class CryptoAssetRepository : ICryptoAssetRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public CryptoAssetRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<CryptoAsset>> GetAllAsync()
        {
            return await _dbContext.CryptoAssets.ToListAsync();
        }

        public async Task<CryptoAsset?> GetByIdAsync(int id)
        {
            return await _dbContext.CryptoAssets.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<CryptoPriceHistory?> GetLastPriceHistoryAsync(int assetId)
        {
            return await _dbContext.CryptoPriceHistories
                        .Where(p => p.CryptoAssetId == assetId)
                        .OrderByDescending(p => p.Date)
                        .FirstOrDefaultAsync();
        }

        public void AddPriceHistory(CryptoPriceHistory priceHistory)
        {
            _dbContext.CryptoPriceHistories.Add(priceHistory);
        }

        public void AddPriceHistories(IEnumerable<CryptoPriceHistory> priceHistories)
        {
            _dbContext.CryptoPriceHistories.AddRange(priceHistories);
        }

        public void AddAsset(CryptoAsset asset)
        {
            _dbContext.CryptoAssets.Add(asset);
        }

        public void UpdateAsset(CryptoAsset asset)
        {
            _dbContext.CryptoAssets.Update(asset);
        }

        public void UpdateAssets(IEnumerable<CryptoAsset> assets)
        {
            _dbContext.CryptoAssets.UpdateRange(assets);
        }

        public async Task<CryptoAsset?> GetByExternalIdAsync(string externalId)
        {
            return await _dbContext.CryptoAssets.FirstOrDefaultAsync(a => a.ExternalId == externalId);
        }

        public async Task<List<CryptoAsset>> GetByExternalIdsAsync(IEnumerable<string> externalIds)
        {
            return await _dbContext.CryptoAssets.Where(a => externalIds.Contains(a.ExternalId)).ToListAsync();
        }

        public async Task<List<AssetInfo>> GetAssetsInfoAsync(IEnumerable<string> externalIds)
        {
            var assets = await _dbContext.CryptoAssets
                .Where(a => externalIds.Contains(a.ExternalId))
                .Select(a => new AssetInfo
                {
                    ExternalId = a.ExternalId,
                    Id = a.Id,
                    Name = a.Name,
                    Symbol = a.Symbol,
                    IconUrl = a.IconUrl,
                    LastPrice = a.PriceHistory
                                    .OrderByDescending(p => p.Date)
                                    .Select(p => (decimal?)p.Price)
                                    .FirstOrDefault()
                })
                .ToListAsync();

            return assets;
        }

        public async Task<Dictionary<int, CryptoPriceHistory>> GetLastPriceHistoriesAsync(IEnumerable<int> assetIds)
        {
            var lastPrices = await _dbContext.CryptoPriceHistories
                .Where(p => assetIds.Contains(p.CryptoAssetId))
                .GroupBy(p => p.CryptoAssetId)
                .Select(g => g.OrderByDescending(p => p.Date).First())
                .ToDictionaryAsync(p => p.CryptoAssetId, p => p);

            return lastPrices;
        }

        public async Task<Dictionary<int, List<CryptoPriceHistory>>> GetTwoLatestPriceHistoriesAsync(IEnumerable<int> assetIds)
        {
            var twoLatestPrices = await _dbContext.CryptoPriceHistories
                .Where(p => assetIds.Contains(p.CryptoAssetId))
                .GroupBy(p => p.CryptoAssetId)
                .Select(g => new
                {
                    AssetId = g.Key,
                    Prices = g.OrderByDescending(p => p.Date).Take(2).ToList()
                })
                .ToDictionaryAsync(x => x.AssetId, x => x.Prices);

            return twoLatestPrices;
        }
    }
}