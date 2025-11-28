using CryptoPriceTraker.Core.Interfaces;
using CryptoPriceTraker.Core.Models;
using CryptoPriceTraker.Core.Proxies;
using CryptoPriceTraker.Modules.Track.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoPriceTraker.Modules.Track.Services
{
    public class CryptoPriceService : ICryptoPriceService
    {
        private readonly ICryptoAssetRepository _cryptoAssetRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinGeckoProxy _coinGeckoProxy;
        private readonly IServiceScopeFactory _scopeFactory;



        public CryptoPriceService(
            ICryptoAssetRepository cryptoAssetRepository,
            IUnitOfWork unitOfWork,
            ICoinGeckoProxy coinGeckoProxy,
            IServiceScopeFactory scopeFactory)
        {
            _cryptoAssetRepository = cryptoAssetRepository;
            _unitOfWork = unitOfWork;
            _coinGeckoProxy = coinGeckoProxy;
            _scopeFactory = scopeFactory;
        }

        public async Task<List<CryptoAssetResponseDto>> UpdatePricesAsync()
        {
            try
            {
                var allCoins = await _coinGeckoProxy.GetCoinList();

                // Ensure uniqueness within the fetched coins based on Name and Symbol
                var distinctCoins = allCoins
                    .GroupBy(c => c.Symbol)
                    .Select(g => g.First())
                    .GroupBy(c => c.Name)
                    .Select(g => g.First())
                    .ToList();

                var cryptoAssets = (await _cryptoAssetRepository.GetAllAsync()).ToList();
                var cryptoAssetsDict = cryptoAssets.ToDictionary(a => a.ExternalId, a => a);
                var existingNames = new HashSet<string>(cryptoAssets.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
                var existingSymbols = new HashSet<string>(cryptoAssets.Select(a => a.Symbol), StringComparer.OrdinalIgnoreCase);

                var newAssets = distinctCoins
                    .Where(c => !cryptoAssetsDict.ContainsKey(c.Id) &&
                                !existingNames.Contains(c.Name) &&
                                !existingSymbols.Contains(c.Symbol))
                    .Select(c => new CryptoAsset { ExternalId = c.Id, Name = c.Name, Symbol = c.Symbol, IconUrl = "" })
                    .ToList();

                if (newAssets.Any())
                {
                    newAssets.ForEach(asset => _cryptoAssetRepository.AddAsset(asset));
                    await _unitOfWork.SaveChangesAsync();
                    cryptoAssets.AddRange(newAssets);
                    newAssets.ForEach(asset => cryptoAssetsDict[asset.ExternalId] = asset);
                }

                var allAssetIdsInDb = cryptoAssets.Select(a => a.Id).ToList();
                var lastPriceHistories = await _cryptoAssetRepository.GetLastPriceHistoriesAsync(allAssetIdsInDb);
                var orderedAssetIds = GetPrioritizedAssetExternalIds(cryptoAssets, lastPriceHistories);

                var batches = orderedAssetIds.Chunk(400);

                var semaphore = new SemaphoreSlim(2);
                var tasks = batches.Select(batch => ProcessBatchAsync(batch, cryptoAssetsDict, semaphore)).ToList();

                var dtoBatches = await Task.WhenAll(tasks);
                return dtoBatches.SelectMany(dtos => dtos).ToList();
            }
            catch (Exception ex)
            {
                return new List<CryptoAssetResponseDto>();
            }
        }

        public async Task<PaginatedResult<CryptoAssetResponseDto>> GetLatestPricesAsync(int pageNumber, int pageSize)
        {
            var allAssets = (await _cryptoAssetRepository.GetAllAsync()).ToList();
            var totalCount = allAssets.Count();

            var assetIds = allAssets.Select(a => a.Id).ToList();
            var twoLatestPriceHistories = await _cryptoAssetRepository.GetTwoLatestPriceHistoriesAsync(assetIds);

            var dtos = new List<CryptoAssetResponseDto>();
            foreach (var asset in allAssets)
            {
                if (twoLatestPriceHistories.TryGetValue(asset.Id, out var histories) && histories.Count >= 1)
                {
                    var currentPrice = histories[0].Price;
                    decimal? previousPrice = histories.Count > 1 ? histories[1].Price : (decimal?)null;

                    decimal changePercentage = 0;
                    if (previousPrice.HasValue && previousPrice.Value > 0)
                    {
                        changePercentage = ((currentPrice - previousPrice.Value) / previousPrice.Value) * 100;
                    }

                    dtos.Add(new CryptoAssetResponseDto
                    {
                        Id = asset.ExternalId,
                        Name = asset.Name,
                        Symbol = asset.Symbol,
                        IconUrl = asset.IconUrl ?? "",
                        CurrentPrice = currentPrice,
                        ChangePercentage = changePercentage,
                        LastUpdated = histories[0].Date
                    });
                }
            }

            var paginatedDtos = dtos.OrderByDescending(dto => dto.CurrentPrice)
                                    .Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();

            return new PaginatedResult<CryptoAssetResponseDto>(paginatedDtos, totalCount, pageNumber, pageSize);
        }

        private async Task<List<CryptoAssetResponseDto>> ProcessBatchAsync(
            IEnumerable<string> batch,
            Dictionary<string, CryptoAsset> cryptoAssetsDict,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            var dtos = new List<CryptoAssetResponseDto>();
            try
            {
                var marketDataList = await _coinGeckoProxy.GetCoinMarket(batch, "usd");

                using (var scope = _scopeFactory.CreateScope())
                {
                    var cryptoAssetRepository = scope.ServiceProvider.GetRequiredService<ICryptoAssetRepository>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var externalIdsInBatch = marketDataList.Select(md => md.Id).ToList();
                    var assetsInfoInBatch = (await cryptoAssetRepository.GetAssetsInfoAsync(externalIdsInBatch))
                                            .ToDictionary(a => a.ExternalId, a => a);

                    var newPriceHistories = new List<CryptoPriceHistory>();
                    var assetsToUpdate = new List<CryptoAsset>();

                    foreach (var marketData in marketDataList)
                    {
                        if (assetsInfoInBatch.TryGetValue(marketData.Id, out var assetInfo))
                        {
                            if (string.IsNullOrEmpty(assetInfo.IconUrl))
                            {
                                var cryptoAssetToUpdate = await cryptoAssetRepository.GetByIdAsync(assetInfo.Id);
                                if (cryptoAssetToUpdate != null)
                                {
                                    cryptoAssetToUpdate.IconUrl = marketData.Image;
                                    assetsToUpdate.Add(cryptoAssetToUpdate); // Add to list for batch update
                                }
                            }

                            var currentPrice = marketData.CurrentPrice;


                            var lastPrice = assetInfo.LastPrice;

                            if (currentPrice > 0 && (lastPrice == null || lastPrice != currentPrice))
                            {
                                newPriceHistories.Add(new CryptoPriceHistory
                                {
                                    CryptoAssetId = assetInfo.Id,
                                    Price = currentPrice,
                                    Date = DateTime.UtcNow
                                });
                            }

                            var changePercentage = (lastPrice != null && lastPrice > 0)
                                ? ((currentPrice - lastPrice) / lastPrice) * 100
                                : 0;

                            dtos.Add(new CryptoAssetResponseDto
                            {
                                Id = assetInfo.ExternalId,
                                Name = assetInfo.Name,
                                Symbol = assetInfo.Symbol,
                                IconUrl = assetInfo.IconUrl ?? "",
                                CurrentPrice = currentPrice,
                                ChangePercentage = changePercentage ?? 0
                            });
                        }
                    }

                    if (newPriceHistories.Any())
                    {
                        cryptoAssetRepository.AddPriceHistories(newPriceHistories);
                    }

                    if (assetsToUpdate.Any())
                    {
                        cryptoAssetRepository.UpdateAssets(assetsToUpdate);
                    }

                    await unitOfWork.SaveChangesAsync();
                }
            }
            finally
            {
                semaphore.Release();
            }
            return dtos;
        }

        private List<string> GetPrioritizedAssetExternalIds(
            List<CryptoAsset> cryptoAssets,
            Dictionary<int, CryptoPriceHistory> lastPriceHistories)
        {
            var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);

            var assetsWithHistory = cryptoAssets
                .Where(asset => lastPriceHistories.ContainsKey(asset.Id))
                .Select(asset => new
                {
                    asset.ExternalId,
                    HistoryDate = lastPriceHistories[asset.Id].Date
                })
                .ToList();

            var noHistoryAssets = cryptoAssets
                .Where(asset => !lastPriceHistories.ContainsKey(asset.Id))
                .Select(asset => asset.ExternalId)
                .ToList();

            var oldHistoryAssets = assetsWithHistory
                .Where(a => a.HistoryDate < thirtyMinutesAgo)
                .OrderBy(a => a.HistoryDate)
                .Select(a => a.ExternalId)
                .ToList();

            var recentHistoryAssets = assetsWithHistory
                .Where(a => a.HistoryDate >= thirtyMinutesAgo)
                .OrderBy(a => a.HistoryDate)
                .Select(a => a.ExternalId)
                .ToList();

            return noHistoryAssets
                .Concat(oldHistoryAssets)
                .Concat(recentHistoryAssets)
                .ToList();
        }
    }
}