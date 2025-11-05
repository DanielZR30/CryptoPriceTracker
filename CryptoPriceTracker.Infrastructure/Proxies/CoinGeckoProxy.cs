using CryptoPriceTraker.Core.Models;
using CryptoPriceTraker.Core.Proxies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoPriceTracker.Infrastructure.Proxies
{
    public class CoinGeckoProxy : ICoinGeckoProxy
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly CoinGeckoSettings _coinGeckoSettings;
        private readonly ILogger<CoinGeckoProxy> _logger;

        public CoinGeckoProxy(HttpClient httpClient, IOptions<CoinGeckoSettings> coinGeckoSettings, ILogger<CoinGeckoProxy> logger)
        {
            _httpClient = httpClient;
            _coinGeckoSettings = coinGeckoSettings.Value;
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            _logger = logger;
        }

        public async Task<IEnumerable<CoinData>> GetCoinList()
        {
            var response = await _httpClient.GetAsync("https://api.coingecko.com/api/v3/coins/list");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve coin list from CoinGecko. Status Code: {StatusCode}. Response: {ResponseContent}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return Enumerable.Empty<CoinData>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var coinData = JsonSerializer.Deserialize<IEnumerable<CoinData>>(json, _serializerOptions);
            return coinData ?? Enumerable.Empty<CoinData>();
        }

        public async Task<Dictionary<string, CoinPrice>> GetPrices(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new Dictionary<string, CoinPrice>();
            }

            var idList = string.Join(",", ids);
            var response = await _httpClient.GetAsync($"https://api.coingecko.com/api/v3/simple/price?ids={idList}&vs_currencies=usd");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve prices from CoinGecko for IDs: {Ids}. Status Code: {StatusCode}. Response: {ResponseContent}", idList, response.StatusCode, await response.Content.ReadAsStringAsync());
                return new Dictionary<string, CoinPrice>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var prices = JsonSerializer.Deserialize<Dictionary<string, CoinPrice>>(json, _serializerOptions);
            return prices ?? new Dictionary<string, CoinPrice>();
        }

        public async Task<IEnumerable<CoinMarketData>> GetCoinMarket(IEnumerable<string> ids, string vs_currency)
        {
            if (ids == null || !ids.Any())
            {
                return Enumerable.Empty<CoinMarketData>();
            }

            var idList = string.Join(",", ids);

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.coingecko.com/api/v3/coins/markets?ids={idList}&vs_currency={vs_currency}");
            request.Headers.Add("x-cg-demo-api-key", _coinGeckoSettings.ApiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve market data from CoinGecko for IDs: {Ids} and currency: {VsCurrency}. Status Code: {StatusCode}. Response: {ResponseContent}", idList, vs_currency, response.StatusCode, await response.Content.ReadAsStringAsync());
                return Enumerable.Empty<CoinMarketData>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var marketData = JsonSerializer.Deserialize<IEnumerable<CoinMarketData>>(json, _serializerOptions);
            return marketData ?? Enumerable.Empty<CoinMarketData>();
        }
    }
}