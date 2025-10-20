using Xunit;
using Moq;
using CryptoPriceTraker.Core.Interfaces;
using CryptoPriceTraker.Core.Models;
using CryptoPriceTraker.Core.Proxies;
using CryptoPriceTraker.Modules.Track.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoPriceTracker.Tests.Modules.Track.Services
{
    public class CryptoPriceServiceTests
    {
        private readonly Mock<ICryptoAssetRepository> _mockCryptoAssetRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICoinGeckoProxy> _mockCoinGeckoProxy;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly CryptoPriceService _cryptoPriceService;

        public CryptoPriceServiceTests()
        {
            _mockCryptoAssetRepository = new Mock<ICryptoAssetRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCoinGeckoProxy = new Mock<ICoinGeckoProxy>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(ICryptoAssetRepository)))
                               .Returns(_mockCryptoAssetRepository.Object);
            mockServiceProvider.Setup(s => s.GetService(typeof(IUnitOfWork)))
                               .Returns(_mockUnitOfWork.Object);

            var mockServiceScope = new Mock<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            _mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);

            _cryptoPriceService = new CryptoPriceService(
                _mockCryptoAssetRepository.Object,
                _mockUnitOfWork.Object,
                _mockCoinGeckoProxy.Object,
                _mockScopeFactory.Object);
        }

        [Fact]
        public void GetPrioritizedAssetExternalIds_ReturnsCorrectOrder()
        {
            var cryptoAssets = new List<CryptoAsset>
            {
                new CryptoAsset { Id = 1, ExternalId = "asset1", Name = "Asset 1", Symbol = "A1" },
                new CryptoAsset { Id = 2, ExternalId = "asset2", Name = "Asset 2", Symbol = "A2" },
                new CryptoAsset { Id = 3, ExternalId = "asset3", Name = "Asset 3", Symbol = "A3" }
            };

            var lastPriceHistories = new Dictionary<int, CryptoPriceHistory>
            {
                { 1, new CryptoPriceHistory { CryptoAssetId = 1, Date = DateTime.UtcNow.AddMinutes(-10) } },
                { 2, new CryptoPriceHistory { CryptoAssetId = 2, Date = DateTime.UtcNow.AddMinutes(-60) } }
            };

            var method = typeof(CryptoPriceService).GetMethod("GetPrioritizedAssetExternalIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method); 

            // Act
            var prioritizedIds = (List<string>)method.Invoke(_cryptoPriceService, new object[] { cryptoAssets, lastPriceHistories });

            // Assert
            Assert.NotNull(prioritizedIds);
            Assert.Equal(3, prioritizedIds.Count);

            // Expected order: Asset 3
            Assert.Equal("asset3", prioritizedIds[0]);
            Assert.Equal("asset2", prioritizedIds[1]);
            Assert.Equal("asset1", prioritizedIds[2]);
        }

        [Fact]
        public async Task UpdatePricesAsync_PrioritizesAssetsWithoutRecentUpdates()
        {
            // Arrange
            var coinList = new List<CoinData>
            {
                new CoinData { Id = "bitcoin", Symbol = "btc", Name = "Bitcoin" },
                new CoinData { Id = "ethereum", Symbol = "eth", Name = "Ethereum" },
                new CoinData { Id = "ripple", Symbol = "xrp", Name = "Ripple" }
            };

            var existingAssets = new List<CryptoAsset>
            {
                new CryptoAsset { Id = 1, ExternalId = "bitcoin", Name = "Bitcoin", Symbol = "btc" },
                new CryptoAsset { Id = 2, ExternalId = "ethereum", Name = "Ethereum", Symbol = "eth" },
                new CryptoAsset { Id = 3, ExternalId = "ripple", Name = "Ripple", Symbol = "xrp" }
            };

            var lastPriceHistories = new Dictionary<int, CryptoPriceHistory>
            {
                { 1, new CryptoPriceHistory { CryptoAssetId = 1, Date = DateTime.UtcNow.AddMinutes(-10) } }, 
                { 2, new CryptoPriceHistory { CryptoAssetId = 2, Date = DateTime.UtcNow.AddMinutes(-60) } } 
            };

            _mockCoinGeckoProxy.Setup(p => p.GetCoinList()).ReturnsAsync(coinList);
            _mockCryptoAssetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(existingAssets);
            _mockCryptoAssetRepository.Setup(r => r.GetLastPriceHistoriesAsync(It.IsAny<List<int>>()))
                                      .ReturnsAsync(lastPriceHistories);
            _mockCoinGeckoProxy.Setup(p => p.GetCoinMarket(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                               .ReturnsAsync(new List<CoinMarketData>());

            // Act
            await _cryptoPriceService.UpdatePricesAsync();

            // Assert
            _mockCoinGeckoProxy.Verify(p => p.GetCoinMarket(It.Is<IEnumerable<string>>(ids => 
                ids.First() == "ethereum" && ids.Last() == "bitcoin"), // Expect ethereum (not recent) before bitcoin (recent)
                It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task GetLatestPricesAsync_ReturnsPaginatedResultsCorrectly()
        {
            // Arrange
            var assets = new List<CryptoAsset>
            {
                new CryptoAsset { Id = 1, ExternalId = "btc", Name = "Bitcoin", Symbol = "BTC" },
                new CryptoAsset { Id = 2, ExternalId = "eth", Name = "Ethereum", Symbol = "ETH" },
                new CryptoAsset { Id = 3, ExternalId = "xrp", Name = "Ripple", Symbol = "XRP" },
                new CryptoAsset { Id = 4, ExternalId = "ltc", Name = "Litecoin", Symbol = "LTC" },
                new CryptoAsset { Id = 5, ExternalId = "ada", Name = "Cardano", Symbol = "ADA" }
            };

            var priceHistories = new Dictionary<int, List<CryptoPriceHistory>>
            {
                { 1, new List<CryptoPriceHistory> { new CryptoPriceHistory { Price = 50000, Date = DateTime.UtcNow } } },
                { 2, new List<CryptoPriceHistory> { new CryptoPriceHistory { Price = 3000, Date = DateTime.UtcNow } } },
                { 3, new List<CryptoPriceHistory> { new CryptoPriceHistory { Price = 1, Date = DateTime.UtcNow } } },
                { 4, new List<CryptoPriceHistory> { new CryptoPriceHistory { Price = 200, Date = DateTime.UtcNow } } },
                { 5, new List<CryptoPriceHistory> { new CryptoPriceHistory { Price = 0.5m, Date = DateTime.UtcNow } } }
            };

            _mockCryptoAssetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(assets);
            _mockCryptoAssetRepository.Setup(r => r.GetTwoLatestPriceHistoriesAsync(It.IsAny<List<int>>()))
                                      .ReturnsAsync(priceHistories);

            // Act
            var resultPage1 = await _cryptoPriceService.GetLatestPricesAsync(1, 2);
            var resultPage2 = await _cryptoPriceService.GetLatestPricesAsync(2, 2);
            var resultPage3 = await _cryptoPriceService.GetLatestPricesAsync(3, 2);

            // Assert Page 1
            Assert.NotNull(resultPage1);
            Assert.Equal(5, resultPage1.TotalCount);
            Assert.Equal(1, resultPage1.PageNumber);
            Assert.Equal(2, resultPage1.PageSize);
            Assert.Equal(2, resultPage1.Items.Count());
            Assert.Equal("Bitcoin", resultPage1.Items.First().Name);
            Assert.Equal("Ethereum", resultPage1.Items.Skip(1).First().Name);

            // Assert Page 2
            Assert.NotNull(resultPage2);
            Assert.Equal(5, resultPage2.TotalCount);
            Assert.Equal(2, resultPage2.PageNumber);
            Assert.Equal(2, resultPage2.PageSize);
            Assert.Equal(2, resultPage2.Items.Count());
            Assert.Equal("Litecoin", resultPage2.Items.First().Name);
            Assert.Equal("Ripple", resultPage2.Items.Skip(1).First().Name);

            // Assert Page 3 (last item)
            Assert.NotNull(resultPage3);
            Assert.Equal(5, resultPage3.TotalCount);
            Assert.Equal(3, resultPage3.PageNumber);
            Assert.Equal(2, resultPage3.PageSize);
            Assert.Single(resultPage3.Items);
            Assert.Equal("Cardano", resultPage3.Items.First().Name);
        }

        [Fact]
        public async Task GetLatestPricesAsync_HandlesEmptyAssets()
        {
            // Arrange
            _mockCryptoAssetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CryptoAsset>());
            _mockCryptoAssetRepository.Setup(r => r.GetTwoLatestPriceHistoriesAsync(It.IsAny<List<int>>()))
                                      .ReturnsAsync(new Dictionary<int, List<CryptoPriceHistory>>());

            // Act
            var result = await _cryptoPriceService.GetLatestPricesAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetLatestPricesAsync_CalculatesChangePercentageCorrectly()
        {
            // Arrange
            var assets = new List<CryptoAsset>
            {
                new CryptoAsset { Id = 1, ExternalId = "btc", Name = "Bitcoin", Symbol = "BTC" }
            };

            var priceHistories = new Dictionary<int, List<CryptoPriceHistory>>
            {
                { 1, new List<CryptoPriceHistory>
                    {
                        new CryptoPriceHistory { Price = 55000, Date = DateTime.UtcNow },
                        new CryptoPriceHistory { Price = 50000, Date = DateTime.UtcNow.AddHours(-1) }
                    }
                }
            };

            _mockCryptoAssetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(assets);
            _mockCryptoAssetRepository.Setup(r => r.GetTwoLatestPriceHistoriesAsync(It.IsAny<List<int>>()))
                                      .ReturnsAsync(priceHistories);

            // Act
            var result = await _cryptoPriceService.GetLatestPricesAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(10m, result.Items.First().ChangePercentage);
        }

        [Fact]
        public async Task GetLatestPricesAsync_HandlesNoPreviousPriceForChangePercentage()
        {
            // Arrange
            var assets = new List<CryptoAsset>
            {
                new CryptoAsset { Id = 1, ExternalId = "btc", Name = "Bitcoin", Symbol = "BTC" }
            };

            var priceHistories = new Dictionary<int, List<CryptoPriceHistory>>
            {
                { 1, new List<CryptoPriceHistory>
                    {
                        new CryptoPriceHistory { Price = 55000, Date = DateTime.UtcNow }
                    }
                }
            };

            _mockCryptoAssetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(assets);
            _mockCryptoAssetRepository.Setup(r => r.GetTwoLatestPriceHistoriesAsync(It.IsAny<List<int>>()))
                                      .ReturnsAsync(priceHistories);

            // Act
            var result = await _cryptoPriceService.GetLatestPricesAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(0m, result.Items.First().ChangePercentage);
        }
    }
}