using CryptoPriceTraker.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoPriceTraker.Core.Proxies
{
    public interface ICoinGeckoProxy
    {
        Task<IEnumerable<CoinData>> GetCoinList();
        Task<Dictionary<string, CoinPrice>> GetPrices(IEnumerable<string> ids);
        Task<IEnumerable<CoinMarketData>> GetCoinMarket(IEnumerable<string> ids, string vs_currency);
    }
}